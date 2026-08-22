using Microsoft.SemanticKernel;

namespace OttoWikiMcp.McpServer.Endpoints;

/// <summary>
/// Endpoints REST simples para o frontend estático (wwwroot/index.html) consumir via fetch().
/// Deliberadamente separados das tools MCP (que falam JSON-RPC via /mcp) — mesma lógica de
/// negócio por baixo (Kernel/WikiPlugin, WorkApi), dois transportes diferentes, exatamente o
/// padrão descrito na pesquisa de arquitetura (ver F:/Projetos/docs).
/// </summary>
public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/wiki/pages", async (Kernel kernel) =>
        {
            var result = await kernel.InvokeAsync("Wiki", "list_wiki_pages");
            var pages = result.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return Results.Ok(pages);
        });

        api.MapGet("/wiki/page", async (string path, Kernel kernel) =>
        {
            var result = await kernel.InvokeAsync("Wiki", "get_wiki_page", new() { ["path"] = path });
            return Results.Ok(new { path, content = result.ToString() });
        });

        api.MapGet("/wiki/search", async (string q, Kernel kernel) =>
        {
            var result = await kernel.InvokeAsync("Wiki", "search_wiki", new() { ["query"] = q });
            return Results.Ok(new { query = q, results = result.ToString() });
        });

        api.MapGet("/tickets", async (IHttpClientFactory httpClientFactory) =>
        {
            var client = httpClientFactory.CreateClient("WorkApi");
            var json = await client.GetStringAsync("/api/tickets");
            return Results.Content(json, "application/json");
        });

        api.MapGet("/institutions", async (IHttpClientFactory httpClientFactory) =>
        {
            var client = httpClientFactory.CreateClient("WorkApi");
            var json = await client.GetStringAsync("/api/institutions");
            return Results.Content(json, "application/json");
        });
    }
}
