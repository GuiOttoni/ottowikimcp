using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Único cliente HTTP que este serviço usa pra chamar uma API pública em tempo real (ver
// FundosController.BuscarCnpj) — todo o resto do domínio de fundos é dado "baked" em JSON.
builder.Services.AddHttpClient("BrasilApi", client =>
{
    client.BaseAddress = new Uri("https://brasilapi.com.br");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("OttoWikiMcp-POC");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/healthz", () => Results.Ok(new { ok = true }));
app.MapControllers();

app.Run();
