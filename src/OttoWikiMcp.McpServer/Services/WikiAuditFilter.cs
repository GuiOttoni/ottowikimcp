using Microsoft.SemanticKernel;

namespace OttoWikiMcp.McpServer.Services;

/// <summary>
/// Auditoria de chamadas de tool: registra QUANDO e QUAL função de Kernel foi chamada, com os
/// argumentos (truncados — conteúdo de página pode ser grande). Cobre search_wiki, get_wiki_page,
/// update_wiki_page, list_wiki_pages(_json), list_wiki_tags — todas passam por
/// <c>kernel.InvokeAsync</c> (ver Tools/WikiTools.cs). <c>ask_wiki</c> NÃO passa mais pelo Kernel
/// (ver WikiAskService), por isso tem seu próprio log ali.
///
/// Limitação honesta: isto registra o QUÊ e o QUANDO, não o QUEM — o MCP desta POC não tem
/// autenticação por usuário (ver decision D6/autenticação no guia de arquitetura), então não há
/// identidade de chamador confiável pra registrar. Resolver isso é o próximo passo, não este.
/// </summary>
public sealed class WikiAuditFilter(ILogger<WikiAuditFilter> logger) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var argsPreview = string.Join(", ", context.Arguments.Select(a => $"{a.Key}={Truncate(a.Value?.ToString())}"));
        logger.LogInformation(
            "AUDIT tool={Plugin}.{Function} args=[{Args}]",
            context.Function.PluginName, context.Function.Name, argsPreview);

        await next(context);

        logger.LogInformation(
            "AUDIT tool={Plugin}.{Function} concluída",
            context.Function.PluginName, context.Function.Name);
    }

    private static string Truncate(string? value, int max = 120)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= max ? value : value[..max] + "...";
    }
}
