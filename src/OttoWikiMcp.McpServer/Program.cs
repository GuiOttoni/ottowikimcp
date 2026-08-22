using Microsoft.SemanticKernel;
using OttoWikiMcp.McpServer.Plugins;
using OttoWikiMcp.McpServer.Services;
using OttoWikiMcp.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GitWikiSync>();
builder.Services.AddSingleton<WikiPlugin>();
builder.Services.AddHostedService<WikiRefreshHostedService>();

// Semantic Kernel: hoje o WikiPlugin só faz busca por texto simples (ver Plugins/WikiPlugin.cs),
// mas já registrado como plugin de Kernel para evoluir pra busca semântica de verdade (embeddings)
// mais tarde sem mudar a superfície das tools MCP.
builder.Services.AddSingleton(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    var kernel = kernelBuilder.Build();
    kernel.Plugins.AddFromObject(sp.GetRequiredService<WikiPlugin>(), "Wiki");
    return kernel;
});

builder.Services.AddHttpClient("WorkApi", client =>
{
    var baseUrl = builder.Configuration["WorkApi:BaseUrl"]
        ?? throw new InvalidOperationException("Config 'WorkApi:BaseUrl' não definida.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<WikiTools>()
    .WithTools<WikiSyncTool>()
    .WithTools<WorkApiTools>();

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { ok = true }));
app.MapMcp("/mcp");

// Garante que a wiki está clonada antes de aceitar tráfego (a primeira tentativa de clone,
// contra uma wiki real do Azure DevOps, é o momento em que o Git Credential Manager abriria o
// navegador para o login interativo — ver GitWikiSync).
using (var scope = app.Services.CreateScope())
{
    var sync = scope.ServiceProvider.GetRequiredService<GitWikiSync>();
    await sync.EnsureClonedAndUpToDateAsync();
}

app.Run();
