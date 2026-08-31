namespace OttoWikiMcp.WorkApiMock;

/// <summary>
/// Domínio de fundos de investimento (mercado financeiro), usado para dar contexto realista de
/// teste ao MCP (perguntas mais complexas do que tickets/instituições de suporte). Os dados desta
/// vez são REAIS: fundos, administradoras e gestoras vêm do registro público da CVM (Dados
/// Abertos, https://dados.cvm.gov.br) e são enriquecidos com dados cadastrais reais via BrasilAPI
/// (https://brasilapi.com.br) — ver <see cref="FundosData"/> para a proveniência exata e as
/// limitações do que é/não é dado real neste conjunto.
/// </summary>
public sealed record TipoMercado(int Id, string Nome, string Descricao);

public sealed record TipoDeFundo(int Id, string Nome, string Descricao);

/// <summary>
/// Instituição financeira REAL (DTVM/banco/gestora), unificada — a mesma empresa frequentemente
/// atua como administradora de um fundo e gestora de outro (ou dos dois papéis no mesmo fundo),
/// então não faz sentido modelar "Administradora" e "Gestora" como tabelas separadas com
/// identidades diferentes para a mesma empresa. <see cref="Papeis"/> guarda os papéis que essa
/// instituição de fato exerce nos fundos deste conjunto de dados (ex.: ["Administradora"],
/// ["Gestora"], ou os dois). Também é a mesma entidade usada como "cliente" nos tickets de
/// suporte (<see cref="Ticket.InstituicaoId"/>) — não existe mais uma tabela `Institution`
/// separada e fictícia.
/// </summary>
public sealed record Instituicao(
    int Id,
    string Nome,
    string Cnpj,
    string? SituacaoCadastral,
    string? DataAbertura,
    string[] Papeis);

public sealed record Fundo(
    int Id,
    string Nome,
    string Cnpj,
    string? CodigoCvm,
    int TipoDeFundoId,
    int TipoMercadoId,
    int? GestoraId,
    int? AdministradoraId,
    decimal PatrimonioLiquido,
    DateOnly? DataInicio,
    string? Benchmark,
    decimal? TaxaAdministracaoPercentual,
    decimal? TaxaPerformancePercentual,
    string Moeda);

public sealed record CotaHistorico(int FundoId, DateOnly Data, decimal ValorCota, decimal PatrimonioLiquido);

/// <summary>
/// Um item do índice de busca por nome/CNPJ — carregado de <c>Data/fundos-search-index.json</c>,
/// um snapshot bem maior (~34 mil fundos) do registro público da CVM do que os <see cref="Fundo"/>
/// "curados" com histórico de cota. Serve só pra busca (<c>GET /api/fundos/buscar</c>), não tem
/// FK pra <see cref="Instituicao"/> nem histórico associado.
/// </summary>
public sealed record FundoBusca(string Cnpj, string Nome, string Tipo, string Administrador, string Gestor);
