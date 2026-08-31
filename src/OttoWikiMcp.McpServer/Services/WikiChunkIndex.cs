using System.Text.RegularExpressions;

namespace OttoWikiMcp.McpServer.Services;

public sealed record WikiChunk(string Path, string Heading, string Text);

public sealed record ScoredChunk(WikiChunk Chunk, double Score);

/// <summary>
/// Formata os pedaços recuperados como contexto de prompt — usado por qualquer implementação de
/// <see cref="IWikiAnswerGenerator"/> que chame um LLM externo (hoje: Gemini e Claude). Extraído
/// aqui pra não duplicar o mesmo formato em cada provedor novo.
/// </summary>
public static class WikiChunkContext
{
    public static string Build(IReadOnlyList<WikiChunk> chunks) =>
        string.Join("\n\n---\n\n", chunks.Select(c => $"[Página: {c.Path} — Seção: {c.Heading}]\n{c.Text}"));
}

/// <summary>
/// Recuperação por relevância sobre a wiki local — o "R" de RAG. Antes disso, <c>ask_wiki</c>
/// reusava <c>search_wiki</c> (substring), que só acerta quando a pergunta contém uma palavra
/// exata do texto. Aqui cada página é quebrada em pedaços por seção (<c>##</c>/<c>###</c>), e a
/// busca usa TF-IDF + similaridade de cosseno sobre esses pedaços — pondera termos raros mais que
/// comuns e lida bem com perguntas de várias palavras, sem precisar de uma API de embeddings
/// (o corpus da wiki é pequeno o bastante pra isso rodar em memória, reconstruído a cada consulta).
///
/// Isto NÃO é embedding neural — é o próximo degrau acima de substring, documentado como tal.
/// Trocar por embeddings de verdade (ver <c>mcp-apis-dinamicas.md</c>/<c>boas-praticas-mcp-docs-rag.md</c>
/// no guia) significa só trocar a implementação de <see cref="Search"/>; a assinatura pública não muda.
/// </summary>
public sealed class WikiChunkIndex(GitWikiSync sync)
{
    private const string Extension = ".md";
    private static readonly Regex HeadingRegex = new(@"^(#{2,3})\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex TokenRegex = new(@"[a-zà-ÿ0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "o", "os", "as", "de", "da", "do", "das", "dos", "e", "que", "para", "com", "um", "uma",
        "em", "no", "na", "nos", "nas", "por", "se", "é", "são", "como", "ao", "à", "mais", "ou",
        "quando", "qual", "quais", "isso", "isto", "esse", "essa", "este", "esta",
    };

    public IReadOnlyList<ScoredChunk> Search(string query, int topK = 5)
    {
        var chunks = BuildChunks();
        if (chunks.Count == 0) return [];

        var queryTerms = Tokenize(query);
        if (queryTerms.Count == 0) return [];

        var docFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var chunkTerms = new List<Dictionary<string, int>>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var terms = Tokenize(chunk.Text + " " + chunk.Heading);
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in terms) counts[t] = counts.GetValueOrDefault(t) + 1;
            chunkTerms.Add(counts);
            foreach (var t in counts.Keys) docFrequency[t] = docFrequency.GetValueOrDefault(t) + 1;
        }

        double Idf(string term) =>
            Math.Log((chunks.Count + 1.0) / (docFrequency.GetValueOrDefault(term, 0) + 1.0)) + 1.0;

        var queryVector = queryTerms
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count() * Idf(g.Key), StringComparer.OrdinalIgnoreCase);
        var queryNorm = Math.Sqrt(queryVector.Values.Sum(w => w * w));
        if (queryNorm == 0) return [];

        var scored = new List<ScoredChunk>(chunks.Count);
        for (var i = 0; i < chunks.Count; i++)
        {
            var counts = chunkTerms[i];
            double dot = 0, chunkNormSq = 0;
            foreach (var (term, count) in counts)
            {
                var weight = count * Idf(term);
                chunkNormSq += weight * weight;
                if (queryVector.TryGetValue(term, out var qWeight)) dot += weight * qWeight;
            }
            var chunkNorm = Math.Sqrt(chunkNormSq);
            var score = chunkNorm == 0 ? 0 : dot / (chunkNorm * queryNorm);
            if (score > 0) scored.Add(new ScoredChunk(chunks[i], score));
        }

        return scored.OrderByDescending(s => s.Score).Take(topK).ToList();
    }

    private static List<string> Tokenize(string text) =>
        TokenRegex.Matches(text)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .ToList();

    private List<WikiChunk> BuildChunks()
    {
        var root = sync.LocalPath;
        if (!Directory.Exists(root)) return [];

        var chunks = new List<WikiChunk>();
        foreach (var file in Directory.EnumerateFiles(root, $"*{Extension}", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}")) continue;

            var relativePath = Path.GetRelativePath(root, file)[..^Extension.Length].Replace(Path.DirectorySeparatorChar, '/');
            var content = StripFrontmatter(File.ReadAllText(file).Replace("\r\n", "\n"));

            var headingMatches = HeadingRegex.Matches(content).ToList();
            if (headingMatches.Count == 0)
            {
                if (content.Trim().Length > 0) chunks.Add(new WikiChunk(relativePath, relativePath, content.Trim()));
                continue;
            }

            for (var i = 0; i < headingMatches.Count; i++)
            {
                var start = headingMatches[i].Index;
                var end = i + 1 < headingMatches.Count ? headingMatches[i + 1].Index : content.Length;
                var heading = headingMatches[i].Groups[2].Value.Trim();
                var body = content[start..end].Trim();
                if (body.Length > 0) chunks.Add(new WikiChunk(relativePath, heading, body));
            }
        }

        return chunks;
    }

    private static string StripFrontmatter(string content)
    {
        var lines = content.Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---") return content;
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---") return string.Join('\n', lines[(i + 1)..]);
        }
        return content;
    }
}
