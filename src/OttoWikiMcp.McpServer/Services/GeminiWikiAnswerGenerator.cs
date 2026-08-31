using System.Text.Json;
using System.Text.Json.Serialization;

namespace OttoWikiMcp.McpServer.Services;

/// <summary>
/// Implementação real de <see cref="IWikiAnswerGenerator"/> via Google Gemini (API Key do
/// AI Studio, plano gratuito) — manda os pedaços recuperados por <see cref="WikiChunkIndex"/>
/// como contexto e pede resposta redigida, respondendo só com base no que foi passado. Registrada
/// em <c>Program.cs</c> só quando <c>GEMINI_API_KEY</c>/<c>GOOGLE_API_KEY</c> está configurada.
/// A chave vai na URL como query param (jeito que a API do Gemini espera), nunca em log/corpo.
/// </summary>
public sealed class GeminiWikiAnswerGenerator(IHttpClientFactory httpClientFactory, IConfiguration config)
    : IWikiAnswerGenerator
{
    private const string DefaultModel = "gemini-3.6-flash";

    public async Task<string> GenerateAsync(string question, IReadOnlyList<WikiChunk> chunks, CancellationToken ct = default)
    {
        var context = WikiChunkContext.Build(chunks);
        var model = config["Gemini:Model"] ?? DefaultModel;
        var apiKey = config["Gemini:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
            ?? throw new InvalidOperationException("Gemini:ApiKey/GEMINI_API_KEY não configurada.");

        var request = new GeminiRequest(
            SystemInstruction: new GeminiContent([
                new GeminiPart(
                    "Você responde perguntas usando SOMENTE o contexto da wiki fornecido pelo usuário. " +
                    "Se a resposta não estiver no contexto, diga claramente que não encontrou. " +
                    "Sempre cite o caminho da página-fonte (ex.: 'Arquitetura/Endpoints-Consumidos') " +
                    "de cada afirmação. Responda em português, de forma direta e estruturada (use bullet points quando fizer sentido)."),
            ]),
            Contents:
            [
                new GeminiContent([new GeminiPart($"Contexto da wiki:\n\n{context}\n\n---\n\nPergunta: {question}")]),
            ]);

        var client = httpClientFactory.CreateClient("Gemini");
        var url = $"/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        using var response = await client.PostAsJsonAsync(url, request, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Gemini API respondeu {(int)response.StatusCode}: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Resposta vazia da API do Gemini.");

        var text = payload.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        return string.IsNullOrWhiteSpace(text) ? "A IA não retornou texto." : text;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record GeminiRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiContent SystemInstruction,
        [property: JsonPropertyName("contents")] GeminiContent[] Contents);

    private sealed record GeminiContent(
        [property: JsonPropertyName("parts")] GeminiPart[] Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);
}
