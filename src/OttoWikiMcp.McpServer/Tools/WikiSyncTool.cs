using System.ComponentModel;
using ModelContextProtocol.Server;
using OttoWikiMcp.McpServer.Services;

namespace OttoWikiMcp.McpServer.Tools;

[McpServerToolType]
public sealed class WikiSyncTool(GitWikiSync sync, ILogger<WikiSyncTool> logger)
{
    [McpServerTool(Name = "sync_wiki"), Description("Força uma atualização (git pull) do clone local da wiki agora, em vez de esperar o próximo sync automático.")]
    public async Task<string> SyncWiki()
    {
        try
        {
            await sync.EnsureClonedAndUpToDateAsync();
            return "Wiki sincronizada com sucesso.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao sincronizar a wiki via tool sync_wiki");
            return $"Falha ao sincronizar: {ex.Message}";
        }
    }
}
