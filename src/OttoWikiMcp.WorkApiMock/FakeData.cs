namespace OttoWikiMcp.WorkApiMock;

/// <summary>
/// Tickets de suporte sintéticos, associados a instituições REAIS (<see cref="FundosData.Instituicoes"/>
/// — mesmo dado unificado usado no domínio de fundos, ver <see cref="FundosData"/>). Os assuntos
/// são cenários administrativos genéricos (solicitações de relatório, dúvidas de prazo, cadastro)
/// — deliberadamente NÃO descrevem nenhuma falha/erro real atribuída a uma instituição real e
/// identificável, para não sugerir um fato negativo sobre uma empresa de verdade.
/// </summary>
public static class FakeData
{
    // Ids em Data/fundos-cvm.json (instituições reais, CNPJ verificado via BrasilAPI):
    // 43 = BB Gestão de Recursos DTVM, 9 = BNY Mellon Serviços Financeiros DTVM, 61 = BTG Pactual Serviços Financeiros DTVM.
    public static readonly List<Ticket> Tickets =
    [
        new(101, 43, "Solicitação de exportação de relatório mensal de posição consolidada", TicketStatus.EmAndamento, TicketPriority.Normal, DateTimeOffset.UtcNow.AddHours(-3)),
        new(102, 43, "Dúvida sobre prazo de liquidação (D+2) de resgate", TicketStatus.Aberto, TicketPriority.Baixa, DateTimeOffset.UtcNow.AddHours(-20)),
        new(103, 9, "Atualização de dados cadastrais de usuário na plataforma", TicketStatus.Resolvido, TicketPriority.Baixa, DateTimeOffset.UtcNow.AddDays(-5)),
        new(104, 61, "Solicitação de acesso para novo usuário do time de compliance", TicketStatus.Aberto, TicketPriority.Normal, DateTimeOffset.UtcNow.AddHours(-1)),
        new(105, 9, "Consulta sobre estrutura de taxas exibida no extrato", TicketStatus.Fechado, TicketPriority.Normal, DateTimeOffset.UtcNow.AddDays(-12)),
    ];
}
