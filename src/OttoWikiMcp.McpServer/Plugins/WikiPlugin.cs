using System.ComponentModel;
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
        var normalized = path.Trim('/').Replace('/', Path.DirectorySeparatorChar);
        var file = Path.Combine(root, normalized + Extension);

        if (!File.Exists(file)) return $"Página não encontrada: {path}";
        return ReadPage(file);
    }

    [KernelFunction("list_wiki_pages")]
    [Description("Lista todas as páginas disponíveis na wiki, com seus caminhos.")]
    public string ListWikiPages()
    {
        var root = sync.LocalPath;
        if (!Directory.Exists(root)) return "Wiki ainda não sincronizada localmente.";

        var pages = EnumeratePages(root).Select(f => ToWikiPath(root, f)).OrderBy(p => p);
        return string.Join("\n", pages);
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
