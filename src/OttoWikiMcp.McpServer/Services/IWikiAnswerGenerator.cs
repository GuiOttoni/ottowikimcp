namespace OttoWikiMcp.McpServer.Services;

/// <summary>
/// O "G" de RAG: dado uma pergunta e os pedaços de wiki já recuperados por
/// <see cref="WikiChunkIndex"/>, gera a resposta final. Ponto de extensão único — trocar de
/// provedor de LLM, ou voltar pro mock, é só trocar qual implementação é registrada em
/// <c>Program.cs</c>; <see cref="WikiAskService"/> não muda.
/// </summary>
public interface IWikiAnswerGenerator
{
    Task<string> GenerateAsync(string question, IReadOnlyList<WikiChunk> chunks, CancellationToken ct = default);
}

/// <summary>
/// Fallback determinístico, sem LLM nem custo: concatena os pedaços recuperados com uma frase de
/// abertura. Usado quando nenhuma chave de LLM está configurada (ver <c>Program.cs</c>) — assim o
/// serviço nunca fica fora do ar por falta de chave, só perde a redação "de verdade".
/// </summary>
public sealed class TemplateWikiAnswerGenerator : IWikiAnswerGenerator
{
    public Task<string> GenerateAsync(string question, IReadOnlyList<WikiChunk> chunks, CancellationToken ct = default)
    {
        var body = string.Join("\n\n", chunks.Select(c => $"### {c.Path} — {c.Heading}\n{Truncate(c.Text, 400)}"));
        return Task.FromResult($"Com base na wiki, encontrei isto (modo sem LLM — configure GEMINI_API_KEY ou ANTHROPIC_API_KEY para respostas redigidas):\n\n{body}");
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max] + "...";
}
