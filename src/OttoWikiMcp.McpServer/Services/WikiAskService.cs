using System.Threading.RateLimiting;

namespace OttoWikiMcp.McpServer.Services;

/// <summary>
/// "Perguntar à IA" sobre a wiki — orquestra as duas metades do RAG: <see cref="WikiChunkIndex"/>
/// (recuperação, TF-IDF sobre pedaços da wiki) e <see cref="IWikiAnswerGenerator"/> (geração —
/// mock determinístico por padrão, Gemini/Claude de verdade quando configurado, ver
/// <c>Program.cs</c>). A assinatura pública (<see cref="AskAsync"/> devolvendo pergunta + resposta
/// + fontes) não muda entre os modos — quem consome (tool MCP e endpoint REST) não sabe nem
/// precisa saber qual dos dois está por trás.
///
/// Também é o único ponto de entrada de <c>ask_wiki</c> (REST e tool MCP passam os dois por
/// aqui), então é o lugar certo pra rate limiting compartilhado e auditoria — sem duplicar em
/// dois lugares.
/// </summary>
public sealed class WikiAskService(WikiChunkIndex index, IWikiAnswerGenerator generator, RateLimiter askRateLimiter, ILogger<WikiAskService> logger)
{
    public async Task<WikiAnswer> AskAsync(string question)
    {
        using var lease = await askRateLimiter.AcquireAsync(1);
        if (!lease.IsAcquired)
        {
            logger.LogWarning("AUDIT ask_wiki rejeitada por rate limit. question={Question}", Truncate(question));
            return new WikiAnswer(
                question,
                "Muitas perguntas em pouco tempo — o limite existe pra proteger a cota gratuita do provedor de LLM. Tente de novo em instantes.",
                []);
        }

        logger.LogInformation("AUDIT ask_wiki question={Question}", Truncate(question));

        var matches = index.Search(question, topK: 5);
        if (matches.Count == 0)
            return new WikiAnswer(question, "Não encontrei nada na wiki sobre isso.", []);

        var chunks = matches.Select(m => m.Chunk).ToList();
        var sources = chunks.Select(c => c.Path).Distinct().ToArray();

        string answer;
        try
        {
            answer = await generator.GenerateAsync(question, chunks);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException or InvalidOperationException)
        {
            // O provedor de LLM (free tier, sem SLA) pode falhar ou demorar demais — melhor
            // devolver os trechos recuperados sem redação do que deixar a request pendurada ou
            // estourar 500 pro frontend. O retrieval (a parte determinística) sempre funcionou.
            logger.LogWarning(ex, "AUDIT ask_wiki: geração via LLM falhou, caindo pro resumo dos trechos recuperados.");
            answer = await new TemplateWikiAnswerGenerator().GenerateAsync(question, chunks);
        }

        return new WikiAnswer(question, answer, sources);
    }

    private static string Truncate(string value, int max = 200) => value.Length <= max ? value : value[..max] + "...";
}

public sealed record WikiAnswer(string Question, string Answer, string[] Sources);
