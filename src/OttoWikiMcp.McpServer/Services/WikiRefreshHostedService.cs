namespace OttoWikiMcp.McpServer.Services;

/// <summary>Mantém o clone local da wiki em dia sem depender de alguém chamar a tool `sync_wiki`.</summary>
public sealed class WikiRefreshHostedService(
    GitWikiSync sync,
    IConfiguration config,
    ILogger<WikiRefreshHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = config.GetValue("Wiki:RefreshIntervalMinutes", 15);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await sync.EnsureClonedAndUpToDateAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Um pull falhando (ex.: sem rede, ou precisa de login interativo de novo)
                // não deve derrubar o servidor MCP — as tools continuam servindo a última
                // versão sincronizada com sucesso.
                logger.LogError(ex, "Falha ao sincronizar a wiki automaticamente — mantendo a última versão local");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
