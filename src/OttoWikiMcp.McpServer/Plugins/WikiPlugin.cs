using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using OttoWikiMcp.McpServer.Services;

namespace OttoWikiMcp.McpServer.Plugins;

/// <summary>
/// Plugin nativo do Semantic Kernel para ler o clone local da wiki (ver <see cref="GitWikiSync"/>).
/// Exposto como funções de Kernel (<c>[KernelFunction]</c>) — hoje usado diretamente pelas tools
/// MCP (ver Tools/WikiTools.cs), mas por estar modelado como plugin de Kernel, dá pra evoluir
/// para busca semântica de verdade mais tarde (embeddings + um conector de LLM/Azure OpenAI)
/// sem trocar a interface pública: troque a implementação de <see cref="SearchWiki"/> por uma
/// busca vetorial, o resto do sistema não muda.
/// </summary>
public sealed class WikiPlugin(GitWikiSync sync)
{
    private const string Extension = ".md";

    [KernelFunction("search_wiki")]
    [Description("Busca por texto nas páginas da wiki (busca simples por substring nos títulos e conteúdo, case-insensitive).")]
    public string SearchWiki([Description("Termo de busca")] string query)
    {
        var root = sync.LocalPath;
        if (!Directory.Exists(root)) return "Wiki ainda não sincronizada localmente.";

        var matches = new List<string>();
        foreach (var file in EnumeratePages(root))
        {
            var content = ReadPage(file);
            if (content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileNameWithoutExtension(file).Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = ToWikiPath(root, file);
                var snippet = ExtractSnippet(content, query);
                matches.Add($"### {relativePath}\n{snippet}");
            }
        }

        return matches.Count == 0
            ? $"Nenhuma página encontrada para \"{query}\"."
            : string.Join("\n\n", matches);
    }

    [KernelFunction("get_wiki_page")]
    [Description("Retorna o conteúdo completo de uma página da wiki pelo caminho (ex.: 'Arquitetura/Fluxo-de-Tickets').")]
    public string GetWikiPage([Description("Caminho da página, sem extensão .md")] string path)
    {
        var root = sync.LocalPath;
        if (!TryResolveSafePath(root, path, out var file))
            return $"Caminho inválido: {path}";

        if (!File.Exists(file)) return $"Página não encontrada: {path}";
        return ReadPage(file);
    }

    [KernelFunction("update_wiki_page")]
    [Description("Cria ou atualiza o conteúdo de uma página da wiki e commita a mudança localmente. Se expectedHash for informado e a página já tiver mudado desde que foi lida, a escrita é recusada (evita que duas edições concorrentes se sobrescrevam silenciosamente).")]
    public async Task<string> UpdateWikiPage(
        [Description("Caminho da página, sem extensão .md")] string path,
        [Description("Conteúdo markdown completo da página")] string content,
        [Description("Hash (SHA-256) do conteúdo no momento em que foi lido, para detecção de conflito de escrita concorrente. Opcional — omitir pula a checagem.")] string? expectedHash = null)
    {
        var root = sync.LocalPath;
        if (!TryResolveSafePath(root, path, out var file))
            return $"Caminho inválido: {path}";

        // Optimistic concurrency: só faz sentido checar se a página já existia (criação nova
        // não tem com o que conflitar) e se quem está escrevendo mandou o hash que leu.
        if (!string.IsNullOrEmpty(expectedHash) && File.Exists(file))
        {
            var currentHash = ComputeHash(ReadPage(file));
            if (!string.Equals(currentHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                return $"CONFLITO: {path} foi alterada por outra edição desde que você a carregou. Recarregue o conteúdo atual antes de salvar de novo.";
        }

        var dir = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(file, content);
        await sync.CommitAllAsync($"docs: atualiza {ToWikiPath(root, file)}");
        return $"Página {path} salva.";
    }

    /// <summary>
    /// SHA-256 do conteúdo, em hex — usado pela detecção de conflito de escrita concorrente
    /// (ver <see cref="UpdateWikiPage"/>). Não é uma KernelFunction: é lido pelo endpoint REST
    /// junto com o conteúdo (ver Endpoints/ApiEndpoints.cs), não precisa ser uma tool MCP própria.
    /// </summary>
    public static string ComputeHash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    [KernelFunction("list_wiki_pages")]
    [Description("Lista todas as páginas da wiki (caminhos podem ter subpastas), com as tags de cada uma quando definidas.")]
    public string ListWikiPages()
    {
        var root = sync.LocalPath;
        if (!Directory.Exists(root)) return "Wiki ainda não sincronizada localmente.";

        var lines = EnumeratePages(root)
            .Select(f => (Path: ToWikiPath(root, f), Tags: ParseTags(ReadPage(f))))
            .OrderBy(p => p.Path)
            .Select(p => p.Tags.Length == 0 ? p.Path : $"{p.Path} [{string.Join(", ", p.Tags)}]");
        return string.Join("\n", lines);
    }

    [KernelFunction("list_wiki_tags")]
    [Description("Lista todas as tags usadas na wiki, com a contagem de páginas por tag (ordenado da mais usada pra menos usada).")]
    public string ListWikiTags()
    {
        var root = sync.LocalPath;
        if (!Directory.Exists(root)) return "Wiki ainda não sincronizada localmente.";

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in EnumeratePages(root))
            foreach (var tag in ParseTags(ReadPage(file)))
                counts[tag] = counts.GetValueOrDefault(tag) + 1;

        return counts.Count == 0
            ? "Nenhuma tag encontrada na wiki."
            : string.Join("\n", counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Select(kv => $"{kv.Key} ({kv.Value})"));
    }

    /// <summary>
    /// Mesma listagem de <see cref="ListWikiPages"/>, mas em JSON (path + tags) — usada só pelo
    /// endpoint REST do frontend, não exposta como tool MCP (as tools falam com um LLM, que lida
    /// melhor com o formato de texto acima).
    /// </summary>
    [KernelFunction("list_wiki_pages_json")]
    [Description("Lista as páginas da wiki em JSON (path + tags), para consumo do frontend.")]
    public string ListWikiPagesJson()
    {
        var root = sync.LocalPath;
        if (!Directory.Exists(root)) return "[]";

        var pages = EnumeratePages(root)
            .Select(f =>
            {
                var content = ReadPage(f);
                return new { path = ToWikiPath(root, f), tags = ParseTags(content), title = ParseTitle(content) };
            })
            .OrderBy(p => p.path);
        return JsonSerializer.Serialize(pages);
    }

    /// <summary>
    /// Título de exibição de uma página: usa <c>title:</c> do frontmatter se existir, senão o
    /// primeiro heading <c># </c> do corpo — assim a árvore/breadcrumb/navegação do frontend não
    /// precisam mostrar o nome cru do arquivo (ex.: "Visao-Geral") quando a página já tem um
    /// título de verdade, acentuado, em algum lugar. Retorna <c>null</c> se nenhum dos dois
    /// existir (o frontend cai pra uma humanização do nome do arquivo nesse caso).
    /// </summary>
    private static string? ParseTitle(string content)
    {
        var lines = content.Split('\n');
        var start = 0;
        if (lines.Length > 0 && lines[0].Trim() == "---")
        {
            for (var i = 1; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed == "---") { start = i + 1; break; }
                if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
                    return trimmed["title:".Length..].Trim().Trim('"');
            }
        }

        for (var i = start; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("# ")) return trimmed[2..].Trim();
        }

        return null;
    }

    /// <summary>
    /// Extrai a lista de tags de um frontmatter YAML simples no topo do arquivo, no formato:
    /// <c>---\ntags: [a, b]\n---</c>. Parser minimalista de propósito (não é YAML completo) —
    /// suficiente para o único campo que a POC usa; se a wiki real já tiver frontmatter mais
    /// rico, considere trocar por uma lib de YAML nesse ponto.
    /// </summary>
    private static string[] ParseTags(string content)
    {
        var lines = content.Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---") return [];

        for (var i = 1; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed == "---") break;
            if (!trimmed.StartsWith("tags:", StringComparison.OrdinalIgnoreCase)) continue;

            var rest = trimmed["tags:".Length..].Trim().Trim('[', ']');
            return rest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return [];
    }

    /// <summary>
    /// Resolve um `path` vindo de fora (tool MCP ou REST) pro arquivo `.md` correspondente,
    /// garantindo que o resultado continua dentro da raiz da wiki. Usado tanto por leitura
    /// (<see cref="GetWikiPage"/>) quanto por escrita (<see cref="UpdateWikiPage"/>) — path
    /// traversal ("../../etc/algo") é um risco em qualquer operação de arquivo indexada por
    /// entrada de usuário/agente, não só em escrita. Antes desta extração, só a escrita tinha
    /// essa checagem; a leitura ficou exposta até ser corrigido.
    /// </summary>
    private static bool TryResolveSafePath(string root, string path, out string file)
    {
        var normalized = path.Trim('/').Replace('/', Path.DirectorySeparatorChar);
        file = Path.GetFullPath(Path.Combine(root, normalized + Extension));
        var rootFull = Path.GetFullPath(root);
        return file.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(file, rootFull, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadPage(string file) => File.ReadAllText(file).Replace("\r\n", "\n");

    private static IEnumerable<string> EnumeratePages(string root) =>
        Directory.EnumerateFiles(root, $"*{Extension}", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}"));

    private static string ToWikiPath(string root, string file) =>
        Path.GetRelativePath(root, file)[..^Extension.Length].Replace(Path.DirectorySeparatorChar, '/');

    private static string ExtractSnippet(string content, string query)
    {
        var idx = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return content.Length > 160 ? content[..160] + "..." : content;

        var start = Math.Max(0, idx - 60);
        var end = Math.Min(content.Length, idx + query.Length + 100);
        return (start > 0 ? "..." : "") + content[start..end].Replace("\n", " ") + (end < content.Length ? "..." : "");
    }
}
