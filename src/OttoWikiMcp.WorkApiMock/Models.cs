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

public sealed record Institution(int Id, string Name, string Plan, DateOnly OnboardedOn);

public sealed record Ticket(
    int Id,
    int InstitutionId,
    string Subject,
    TicketStatus Status,
    TicketPriority Priority,
    DateTimeOffset CreatedAt);
