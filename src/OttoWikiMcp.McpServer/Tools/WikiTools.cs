using System.ComponentModel;
using ModelContextProtocol.Server;
using OttoWikiMcp.McpServer.Plugins;

namespace OttoWikiMcp.McpServer.Tools;

[McpServerToolType]
public sealed class WikiTools(WikiPlugin wiki)
{
    [McpServerTool(Name = "search_wiki"), Description("Busca páginas da wiki por texto.")]
    public string SearchWiki([Description("Termo de busca")] string query) => wiki.SearchWiki(query);

    [McpServerTool(Name = "get_wiki_page"), Description("Retorna o conteúdo completo de uma página da wiki.")]
    public string GetWikiPage([Description("Caminho da página, ex.: 'Arquitetura/Fluxo-de-Tickets'")] string path) =>
        wiki.GetWikiPage(path);

    [McpServerTool(Name = "list_wiki_pages"), Description("Lista todas as páginas da wiki.")]
    public string ListWikiPages() => wiki.ListWikiPages();
}
