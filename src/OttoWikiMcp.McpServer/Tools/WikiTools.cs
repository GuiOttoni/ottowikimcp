using System.ComponentModel;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using OttoWikiMcp.McpServer.Services;

namespace OttoWikiMcp.McpServer.Tools;

/// <summary>
/// As tools MCP chamam o <see cref="WikiPlugin"/> através do <see cref="Kernel"/> do Semantic
/// Kernel (não direto), invocando as funções pelo nome (<c>kernel.InvokeAsync</c>) — o mesmo
/// caminho que uma orquestração de LLM via SK usaria. Isso deixa o Kernel genuinamente no meio
/// do caminho, não só registrado sem uso: dá pra plugar, por exemplo, um planner do SK na
/// frente disso depois sem precisar tocar nas tools MCP.
/// </summary>
[McpServerToolType]
public sealed class WikiTools(Kernel kernel, WikiAskService askService)
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

    [McpServerTool(Name = "list_wiki_tags"), Description("Lista todas as tags usadas na wiki, com contagem de páginas por tag.")]
    public async Task<string> ListWikiTags()
    {
        var result = await kernel.InvokeAsync(PluginName, "list_wiki_tags");
        return result.ToString();
    }

    [McpServerTool(Name = "update_wiki_page"), Description("Cria ou atualiza uma página da wiki e commita a mudança. Se expectedHash for informado, a escrita é recusada quando a página já mudou desde a leitura (evita sobrescrever uma edição concorrente).")]
    public async Task<string> UpdateWikiPage(
        [Description("Caminho da página, ex.: 'Arquitetura/Fluxo-de-Tickets'")] string path,
        [Description("Conteúdo markdown completo da página")] string content,
        [Description("Hash (SHA-256) do conteúdo lido antes de editar — opcional, ver get_wiki_page")] string? expectedHash = null)
    {
        var args = new KernelArguments { ["path"] = path, ["content"] = content };
        if (expectedHash is not null) args["expectedHash"] = expectedHash;
        var result = await kernel.InvokeAsync(PluginName, "update_wiki_page", args);
        return result.ToString();
    }

    [McpServerTool(Name = "ask_wiki"), Description("Pergunta em linguagem natural sobre o conteúdo da wiki — recuperação por relevância (TF-IDF sobre pedaços da wiki) + resposta redigida por LLM (Gemini ou Claude, conforme a chave configurada; senão um resumo determinístico dos trechos recuperados). Sujeito a rate limit compartilhado com o endpoint REST.")]
    public async Task<string> AskWiki([Description("Pergunta")] string question)
    {
        var result = await askService.AskAsync(question);
        return result.Answer;
    }
}
