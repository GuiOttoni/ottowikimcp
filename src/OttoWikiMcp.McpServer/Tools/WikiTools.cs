using System.ComponentModel;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;

namespace OttoWikiMcp.McpServer.Tools;

/// <summary>
/// As tools MCP chamam o <see cref="WikiPlugin"/> através do <see cref="Kernel"/> do Semantic
/// Kernel (não direto), invocando as funções pelo nome (<c>kernel.InvokeAsync</c>) — o mesmo
/// caminho que uma orquestração de LLM via SK usaria. Isso deixa o Kernel genuinamente no meio
/// do caminho, não só registrado sem uso: dá pra plugar, por exemplo, um planner do SK na
/// frente disso depois sem precisar tocar nas tools MCP.
/// </summary>
[McpServerToolType]
public sealed class WikiTools(Kernel kernel)
{
    private const string PluginName = "Wiki";

    [McpServerTool(Name = "search_wiki"), Description("Busca páginas da wiki por texto.")]
    public async Task<string> SearchWiki([Description("Termo de busca")] string query)
    {
        var result = await kernel.InvokeAsync(PluginName, "search_wiki", new() { ["query"] = query });
        return result.ToString();
    }

    [McpServerTool(Name = "get_wiki_page"), Description("Retorna o conteúdo completo de uma página da wiki.")]
    public async Task<string> GetWikiPage([Description("Caminho da página, ex.: 'Arquitetura/Fluxo-de-Tickets'")] string path)
    {
        var result = await kernel.InvokeAsync(PluginName, "get_wiki_page", new() { ["path"] = path });
        return result.ToString();
    }

    [McpServerTool(Name = "list_wiki_pages"), Description("Lista todas as páginas da wiki.")]
    public async Task<string> ListWikiPages()
    {
        var result = await kernel.InvokeAsync(PluginName, "list_wiki_pages");
        return result.ToString();
    }
}
