namespace OttoWikiMcp.WorkApiMock;

/// <summary>
/// Dados fictícios, só para a POC do OttoWikiMcp ter algo real para consultar via HTTP —
/// representa o tipo de API interna (tickets, instituições) que existiria de verdade no
/// ambiente de trabalho. Em memória de propósito: não é para virar uma API de produção.
/// </summary>
public static class FakeData
{
    public static readonly List<Institution> Institutions =
    [
        new(1, "Hospital Vida Nova", "Enterprise", new DateOnly(2023, 3, 14)),
        new(2, "Clínica Bem Estar", "Pro", new DateOnly(2024, 7, 2)),
        new(3, "Instituto Saúde Total", "Starter", new DateOnly(2025, 1, 20)),
    ];

    public static readonly List<Ticket> Tickets =
    [
        new(101, 1, "Login intermitente para usuários do plano Enterprise", TicketStatus.EmAndamento, TicketPriority.Critica, DateTimeOffset.UtcNow.AddHours(-3)),
        new(102, 1, "Relatório mensal não gera PDF", TicketStatus.Aberto, TicketPriority.Alta, DateTimeOffset.UtcNow.AddHours(-20)),
        new(103, 2, "Dúvida sobre limite de usuários do plano Pro", TicketStatus.Resolvido, TicketPriority.Baixa, DateTimeOffset.UtcNow.AddDays(-5)),
        new(104, 3, "Erro 500 ao importar planilha de pacientes", TicketStatus.Aberto, TicketPriority.Alta, DateTimeOffset.UtcNow.AddHours(-1)),
        new(105, 2, "Solicitação de upgrade para plano Enterprise", TicketStatus.Fechado, TicketPriority.Normal, DateTimeOffset.UtcNow.AddDays(-12)),
    ];
}
