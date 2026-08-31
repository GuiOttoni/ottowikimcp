using Microsoft.SemanticKernel;
using OttoWikiMcp.McpServer.Plugins;
using OttoWikiMcp.McpServer.Services;

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
            var result = await kernel.InvokeAsync("Wiki", "list_wiki_pages_json");
            return Results.Content(result.ToString(), "application/json");
        });

        api.MapGet("/wiki/page", async (string path, Kernel kernel) =>
        {
            var result = await kernel.InvokeAsync("Wiki", "get_wiki_page", new() { ["path"] = path });
            var content = result.ToString();
            return Results.Ok(new { path, content, hash = WikiPlugin.ComputeHash(content) });
        });

        api.MapGet("/wiki/search", async (string q, Kernel kernel) =>
        {
            var result = await kernel.InvokeAsync("Wiki", "search_wiki", new() { ["query"] = q });
            return Results.Ok(new { query = q, results = result.ToString() });
        });

        api.MapPut("/wiki/page", async (UpdatePageRequest body, Kernel kernel) =>
        {
            var args = new KernelArguments { ["path"] = body.Path, ["content"] = body.Content };
            if (body.ExpectedHash is not null) args["expectedHash"] = body.ExpectedHash;
            var result = await kernel.InvokeAsync("Wiki", "update_wiki_page", args);
            var message = result.ToString();

            // Conflito de escrita concorrente vira 409, não 200 — o frontend precisa distinguir
            // "salvou" de "não salvou porque alguém mexeu antes" sem parsear o texto da mensagem.
            if (message.StartsWith("CONFLITO", StringComparison.Ordinal))
                return Results.Conflict(new { message });

            var hash = WikiPlugin.ComputeHash(body.Content);
            return Results.Ok(new { message, hash });
        });

        api.MapPost("/wiki/ask", async (AskRequest body, WikiAskService askService) =>
        {
            var result = await askService.AskAsync(body.Question);
            return Results.Ok(result);
        });

        api.MapGet("/tickets", async (IHttpClientFactory httpClientFactory) =>
        {
            var client = httpClientFactory.CreateClient("WorkApi");
            var json = await client.GetStringAsync("/api/tickets");
            return Results.Content(json, "application/json");
        });

        // Proxy genérico pro domínio de fundos/instituições do WorkApiMock — o frontend consome
        // tudo isso pelo mesmo padrão (fetch same-origin em /api/*), sem precisar conhecer o
        // WorkApiMock diretamente.
        api.MapGet("/fundos", (HttpRequest req, IHttpClientFactory f) => ProxyGet(f, $"/api/fundos{req.QueryString}"));
        api.MapGet("/fundos/{id:int}", (int id, IHttpClientFactory f) => ProxyGet(f, $"/api/fundos/{id}"));
        api.MapGet("/fundos/{id:int}/historico", (int id, IHttpClientFactory f) => ProxyGet(f, $"/api/fundos/{id}/historico"));
        api.MapGet("/fundos/tipos", (IHttpClientFactory f) => ProxyGet(f, "/api/fundos/tipos"));
        api.MapGet("/fundos/mercados", (IHttpClientFactory f) => ProxyGet(f, "/api/fundos/mercados"));
        api.MapGet("/fundos/instituicoes", (HttpRequest req, IHttpClientFactory f) => ProxyGet(f, $"/api/fundos/instituicoes{req.QueryString}"));
        api.MapGet("/fundos/instituicoes/{id:int}", (int id, IHttpClientFactory f) => ProxyGet(f, $"/api/fundos/instituicoes/{id}"));
        api.MapGet("/fundos/gestoras", (IHttpClientFactory f) => ProxyGet(f, "/api/fundos/gestoras"));
        api.MapGet("/fundos/administradoras", (IHttpClientFactory f) => ProxyGet(f, "/api/fundos/administradoras"));
        api.MapGet("/fundos/buscar", (HttpRequest req, IHttpClientFactory f) => ProxyGet(f, $"/api/fundos/buscar{req.QueryString}"));
        api.MapGet("/fundos/buscar-cnpj/{cnpj}", (string cnpj, IHttpClientFactory f) => ProxyGet(f, $"/api/fundos/buscar-cnpj/{cnpj}"));
    }

    private static async Task<IResult> ProxyGet(IHttpClientFactory httpClientFactory, string path)
    {
        var client = httpClientFactory.CreateClient("WorkApi");
        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
    }
}

public sealed record UpdatePageRequest(string Path, string Content, string? ExpectedHash = null);

public sealed record AskRequest(string Question);
