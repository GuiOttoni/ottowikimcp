namespace OttoWikiMcp.WorkApiMock;

public enum TicketStatus
{
    Aberto,
    EmAndamento,
    Resolvido,
    Fechado,
}

public enum TicketPriority
{
    Baixa,
    Normal,
    Alta,
    Critica,
}

public sealed record Ticket(
    int Id,
    int InstituicaoId,
    string Subject,
    TicketStatus Status,
    TicketPriority Priority,
    DateTimeOffset CreatedAt);
