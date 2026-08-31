using System.Threading.RateLimiting;
using Microsoft.SemanticKernel;
using OttoWikiMcp.McpServer.Endpoints;
using OttoWikiMcp.McpServer.Plugins;
using OttoWikiMcp.McpServer.Services;
using OttoWikiMcp.McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GitWikiSync>();
builder.Services.AddSingleton<WikiPlugin>();
builder.Services.AddSingleton<WikiChunkIndex>();
builder.Services.AddSingleton<WikiAuditFilter>();
builder.Services.AddSingleton<WikiAskService>();
builder.Services.AddHostedService<WikiRefreshHostedService>();

// Rate limit compartilhado por ask_wiki (REST e tool MCP passam pelo mesmo WikiAskService) —
// protege a cota gratuita do provedor de LLM contra uso excessivo. Fixed window simples, sem
// distinção por chamador (a POC não tem identidade de chamador — ver WikiAuditFilter): 8
// perguntas por minuto pro servidor inteiro. QueueLimit 0 = rejeita na hora em vez de enfileirar,
// resposta imediata é melhor UX do que fazer alguém esperar numa fila.
builder.Services.AddSingleton<RateLimiter>(_ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
{
    PermitLimit = 8,
    Window = TimeSpan.FromMinutes(1),
    QueueLimit = 0,
}));

// RAG "de verdade" pra ask_wiki: WikiChunkIndex cuida da recuperação (sempre ligado, sem custo).
// A geração da resposta é plugável — Gemini (plano gratuito) tem prioridade se configurado,
// senão Claude, senão o mock determinístico (nunca fica fora do ar por falta de chave). Nenhuma
// chave é lida de arquivo versionado — só de configuração de ambiente/Secret do k8s (ver
// k8s/mcpserver.yaml: env Gemini__ApiKey via secretKeyRef). Trocar de provedor é só registrar
// outra implementação de IWikiAnswerGenerator aqui.
var geminiApiKey = builder.Configuration["Gemini:ApiKey"]
    ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
var anthropicApiKey = builder.Configuration["Anthropic:ApiKey"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    builder.Services.AddHttpClient("Gemini", client =>
    {
        client.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
        // Sem timeout, uma chamada lenta do free tier (observado: 76s numa chamada real) fica
        // pendurada até o default de 100s do HttpClient — 30s é o bastante pra uma resposta de
        // RAG e falha rápido o suficiente pra WikiAskService cair no fallback (ver AskAsync).
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddSingleton<IWikiAnswerGenerator, GeminiWikiAnswerGenerator>();
}
else if (!string.IsNullOrWhiteSpace(anthropicApiKey))
{
    builder.Services.AddHttpClient("Anthropic", client =>
    {
        client.BaseAddress = new Uri("https://api.anthropic.com");
        client.DefaultRequestHeaders.Add("x-api-key", anthropicApiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddSingleton<IWikiAnswerGenerator, AnthropicWikiAnswerGenerator>();
}
else
{
    builder.Services.AddSingleton<IWikiAnswerGenerator, TemplateWikiAnswerGenerator>();
}

// Semantic Kernel: hoje o WikiPlugin só faz busca por texto simples (ver Plugins/WikiPlugin.cs),
// mas já registrado como plugin de Kernel para evoluir pra busca semântica de verdade (embeddings)
// mais tarde sem mudar a superfície das tools MCP.
builder.Services.AddSingleton(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    var kernel = kernelBuilder.Build();
    kernel.Plugins.AddFromObject(sp.GetRequiredService<WikiPlugin>(), "Wiki");
    // Auditoria: um filtro só, registrado uma vez, cobre TODAS as funções do Kernel (search_wiki,
    // get_wiki_page, update_wiki_page, list_wiki_pages(_json), list_wiki_tags) sem precisar de
    // log manual espalhado em cada tool — ver Services/WikiAuditFilter.cs.
    kernel.FunctionInvocationFilters.Add(sp.GetRequiredService<WikiAuditFilter>());
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

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/healthz", () => Results.Ok(new { ok = true }));
app.MapMcp("/mcp");
app.MapApiEndpoints();
app.MapMcpInfoEndpoints();
// Rotas do frontend (React Router) como /docs/Arquitetura são só client-side — sem isto, um
// refresh direto nessa URL cairia em 404 no ASP.NET Core em vez de servir o index.html do SPA.
app.MapFallbackToFile("index.html");

// Garante que a wiki está clonada antes de aceitar tráfego (a primeira tentativa de clone,
// contra uma wiki real do Azure DevOps, é o momento em que o Git Credential Manager abriria o
// navegador para o login interativo — ver GitWikiSync).
using (var scope = app.Services.CreateScope())
{
    var sync = scope.ServiceProvider.GetRequiredService<GitWikiSync>();
    await sync.EnsureClonedAndUpToDateAsync();
}

app.Run();
