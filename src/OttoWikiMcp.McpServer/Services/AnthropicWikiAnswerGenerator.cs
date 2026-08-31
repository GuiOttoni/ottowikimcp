using System.Text.Json;
using System.Text.Json.Serialization;

namespace OttoWikiMcp.McpServer.Services;

/// <summary>
/// Implementação real de <see cref="IWikiAnswerGenerator"/>: manda os pedaços recuperados por
/// <see cref="WikiChunkIndex"/> como contexto pro Claude (Anthropic Messages API) e pede uma
/// resposta redigida, respondendo só com base no que foi passado (evita alucinar sobre a wiki).
/// Registrada em <c>Program.cs</c> só quando <c>ANTHROPIC_API_KEY</c> está configurada — sem
/// chave, o serviço cai automaticamente pra <see cref="TemplateWikiAnswerGenerator"/>.
/// </summary>
public sealed class AnthropicWikiAnswerGenerator(IHttpClientFactory httpClientFactory, IConfiguration config)
    : IWikiAnswerGenerator
{
    private const string DefaultModel = "claude-3-5-haiku-20241022";

    public async Task<string> GenerateAsync(string question, IReadOnlyList<WikiChunk> chunks, CancellationToken ct = default)
    {
        var context = WikiChunkContext.Build(chunks);
        var model = config["Anthropic:Model"] ?? DefaultModel;
        var request = new AnthropicRequest(
            model,
            1024,
            "Você responde perguntas usando SOMENTE o contexto da wiki fornecido abaixo. " +
            "Se a resposta não estiver no contexto, diga claramente que não encontrou. " +
            "Sempre cite o caminho da página-fonte (ex.: 'Arquitetura/Endpoints-Consumidos') " +
            "de cada afirmação. Responda em português, de forma direta e estruturada (use bullet points quando fizer sentido).",
            [new AnthropicMessage("user", $"Contexto da wiki:\n\n{context}\n\n---\n\nPergunta: {question}")]);

        var client = httpClientFactory.CreateClient("Anthropic");
        using var response = await client.PostAsJsonAsync("/v1/messages", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AnthropicResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Resposta vazia da API da Anthropic.");

        var text = payload.Content.FirstOrDefault(c => c.Type == "text")?.Text;
        return string.IsNullOrWhiteSpace(text) ? "A IA não retornou texto." : text;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record AnthropicRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] AnthropicMessage[] Messages);

    private sealed record AnthropicMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record AnthropicResponse(
        [property: JsonPropertyName("content")] List<AnthropicContentBlock> Content);

    private sealed record AnthropicContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
