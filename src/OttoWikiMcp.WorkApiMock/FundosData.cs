using System.Text.Json;

namespace OttoWikiMcp.WorkApiMock;

/// <summary>
/// Carrega o domínio de fundos de investimento a partir de dados REAIS "baked" em JSON
/// (<c>Data/fundos-cvm.json</c>, <c>Data/historico-cotas-cvm.json</c>,
/// <c>Data/fundos-search-index.json</c>) — não são dados fictícios. Fonte: CVM Dados Abertos
/// (<c>https://dados.cvm.gov.br</c>, arquivos <c>registro_fundo.csv</c>/<c>registro_classe.csv</c>
/// e <c>inf_diario_fi_*.zip</c>) para o registro dos fundos e o histórico de cota/patrimônio, e
/// BrasilAPI (<c>https://brasilapi.com.br/api/cnpj/v1/{cnpj}</c>) para os dados cadastrais reais
/// (razão social, situação, data de abertura) das instituições. Os dados foram consultados uma
/// vez e congelados em JSON (ver campo <c>meta</c> em cada arquivo pra data da consulta) — o
/// serviço não chama essas APIs em runtime para o domínio principal, pra não depender de
/// disponibilidade externa nem repetir custo de rede a cada request. A única chamada em tempo
/// real que este serviço faz é a busca por CNPJ específico (<see cref="Controllers.FundosController.BuscarCnpj"/>),
/// deliberadamente — é o cenário em que "dado fresco" vale mais que "dado rápido".
///
/// Carregamento via <see cref="JsonSerializer.Deserialize{TValue}(System.IO.Stream, JsonSerializerOptions?)"/>
/// direto num <see cref="FileStream"/> (não via <see cref="JsonDocument"/> + <c>GetRawText()</c>)
/// — o índice de busca (<c>fundos-search-index.json</c>) tem ~34 mil registros/~8MB, e o caminho
/// JsonDocument-depois-reserializa-em-string duplicava a memória de pico o suficiente pra estourar
/// o limite do pod no Kubernetes (128Mi) com <see cref="OutOfMemoryException"/>.
///
/// Limitações conhecidas, deliberadas (não invente dado onde a fonte pública não tinha):
/// <list type="bullet">
/// <item>Só fundos <b>onshore</b> — a CVM não registra fundos offshore, não existe fonte pública
/// gratuita equivalente para isso. Fundos offshore só aparecem no conteúdo educacional da wiki,
/// nunca neste conjunto de dados "real".</item>
/// <item><c>Benchmark</c>, <c>TaxaAdministracaoPercentual</c> e <c>TaxaPerformancePercentual</c>
/// não constam no dataset público usado (CAD/INF_DIARIO) — ficam <c>null</c>, não zero e não
/// inventados.</item>
/// <item>Histórico de cota (<see cref="HistoricoCotas"/>) só existe para fundos do tipo
/// FIF-padrão (Renda Fixa/Ações/Multimercado/Cambial); FIDC e FII reportam por um regime CVM
/// diferente, não coberto pelo arquivo <c>inf_diario_fi</c> usado aqui.</item>
/// <item><see cref="Instituicao"/> unifica administradora/gestora — a mesma empresa pode
/// aparecer com os dois papéis (ver <see cref="Instituicao.Papeis"/>), refletindo a estrutura
/// real do mercado (administrador = responsável legal perante a CVM, gestor = quem decide os
/// investimentos — frequentemente empresas diferentes, às vezes a mesma).</item>
/// </list>
/// </summary>
public static class FundosData
{
    public static readonly List<TipoMercado> TiposMercado;
    public static readonly List<TipoDeFundo> TiposDeFundo;
    public static readonly List<Instituicao> Instituicoes;
    public static readonly List<Fundo> Fundos;
    public static readonly List<CotaHistorico> HistoricoCotas;
    public static readonly List<FundoBusca> IndiceBusca;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    static FundosData()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");

        var fundosFile = ReadJsonFile<FundosCvmFile>(Path.Combine(dataDir, "fundos-cvm.json"));
        TiposMercado = fundosFile.TiposMercado;
        TiposDeFundo = fundosFile.TiposDeFundo;
        Instituicoes = fundosFile.Instituicoes;
        Fundos = fundosFile.Fundos;

        var historicoFile = ReadJsonFile<HistoricoFile>(Path.Combine(dataDir, "historico-cotas-cvm.json"));
        var historico = new List<CotaHistorico>();
        foreach (var (fundoIdText, pontos) in historicoFile.HistoricoPorFundoId)
        {
            var fundoId = int.Parse(fundoIdText);
            foreach (var p in pontos)
                historico.Add(new CotaHistorico(fundoId, p.Data, p.ValorCota, p.PatrimonioLiquido));
        }
        HistoricoCotas = historico;

        var searchFile = ReadJsonFile<SearchIndexFile>(Path.Combine(dataDir, "fundos-search-index.json"));
        IndiceBusca = searchFile.Fundos;
    }

    private static T ReadJsonFile<T>(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, JsonOpts)
            ?? throw new InvalidOperationException($"Falha ao desserializar {path}.");
    }

    private sealed record FundosCvmFile(
        List<TipoMercado> TiposMercado,
        List<TipoDeFundo> TiposDeFundo,
        List<Instituicao> Instituicoes,
        List<Fundo> Fundos);

    private sealed record HistoricoFile(Dictionary<string, List<CotaHistoricoPonto>> HistoricoPorFundoId);

    private sealed record CotaHistoricoPonto(DateOnly Data, decimal ValorCota, decimal PatrimonioLiquido);

    private sealed record SearchIndexFile(List<FundoBusca> Fundos);
}
