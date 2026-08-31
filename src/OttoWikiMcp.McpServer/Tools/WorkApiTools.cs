using System.ComponentModel;
using System.Net.Http.Json;
using ModelContextProtocol.Server;

namespace OttoWikiMcp.McpServer.Tools;

/// <summary>
/// Tools que consultam a API interna de trabalho (tickets/instituições). Nesta POC aponta pro
/// OttoWikiMcp.WorkApiMock (dados fictícios); no ambiente real, a mesma forma de tool bateria
/// nas APIs internas de verdade — só o `BaseAddress` do HttpClient muda.
/// </summary>
[McpServerToolType]
public sealed class WorkApiTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("WorkApi");

    [McpServerTool(Name = "list_tickets"), Description("Lista tickets de suporte, com filtro opcional por status ou instituição.")]
    public async Task<string> ListTickets(
        [Description("Status: Aberto, EmAndamento, Resolvido ou Fechado (opcional)")] string? status = null,
        [Description("Id da instituição (opcional)")] int? instituicaoId = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (instituicaoId is not null) query.Add($"instituicaoId={instituicaoId}");
        var qs = query.Count > 0 ? "?" + string.Join('&', query) : "";

        var response = await Client.GetStringAsync($"/api/tickets{qs}");
        return response;
    }

    [McpServerTool(Name = "get_ticket"), Description("Retorna um ticket específico pelo id.")]
    public async Task<string> GetTicket([Description("Id do ticket")] int id)
    {
        var response = await Client.GetAsync($"/api/tickets/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : $"Ticket {id} não encontrado.";
    }

    [McpServerTool(Name = "list_institutions"), Description("Lista todas as instituições (administradoras e/ou gestoras de fundos reais, registradas na CVM).")]
    public async Task<string> ListInstitutions([Description("Filtra por papel: 'Administradora' ou 'Gestora' (opcional)")] string? papel = null)
    {
        var qs = string.IsNullOrWhiteSpace(papel) ? "" : $"?papel={Uri.EscapeDataString(papel)}";
        return await Client.GetStringAsync($"/api/fundos/instituicoes{qs}");
    }

    [McpServerTool(Name = "get_institution"), Description("Retorna uma instituição específica pelo id.")]
    public async Task<string> GetInstitution([Description("Id da instituição")] int id)
    {
        var response = await Client.GetAsync($"/api/fundos/instituicoes/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : $"Instituição {id} não encontrada.";
    }

    [McpServerTool(Name = "find_institution"), Description("Acha uma instituição (administradora/gestora) pelo nome, entre as instituições cadastradas nesta POC (não é busca ampla — ver find_fund pra isso).")]
    public async Task<string> FindInstitution([Description("Termo de busca no nome")] string query)
    {
        var response = await Client.GetStringAsync("/api/fundos/instituicoes");
        using var doc = System.Text.Json.JsonDocument.Parse(response);
        var matches = doc.RootElement.EnumerateArray()
            .Where(e => e.GetProperty("nome").GetString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            .Select(e => e.GetRawText());
        var results = string.Join("\n", matches);
        return string.IsNullOrEmpty(results) ? $"Nenhuma instituição encontrada para \"{query}\"." : results;
    }

    [McpServerTool(Name = "lookup_cnpj"), Description("Consulta AO VIVO um CNPJ na BrasilAPI (dado cadastral fresco: razão social, situação, data de abertura) — não depende do que já está cadastrado nesta POC.")]
    public async Task<string> LookupCnpj([Description("CNPJ (com ou sem pontuação)")] string cnpj)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        var response = await Client.GetAsync($"/api/fundos/buscar-cnpj/{digits}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : $"CNPJ {cnpj} não encontrado na BrasilAPI.";
    }

    [McpServerTool(Name = "list_funds"), Description("Lista fundos de investimento, com filtro opcional por tipo de fundo ou tipo de mercado (onshore/offshore).")]
    public async Task<string> ListFunds(
        [Description("Id do tipo de fundo (ver list_fund_types) — opcional")] int? tipoDeFundoId = null,
        [Description("Id do tipo de mercado: 1=Onshore, 2=Offshore (ver list_market_types) — opcional")] int? tipoMercadoId = null)
    {
        var query = new List<string>();
        if (tipoDeFundoId is not null) query.Add($"tipoDeFundoId={tipoDeFundoId}");
        if (tipoMercadoId is not null) query.Add($"tipoMercadoId={tipoMercadoId}");
        var qs = query.Count > 0 ? "?" + string.Join('&', query) : "";
        return await Client.GetStringAsync($"/api/fundos{qs}");
    }

    [McpServerTool(Name = "get_fund"), Description("Retorna um fundo de investimento específico pelo id.")]
    public async Task<string> GetFund([Description("Id do fundo")] int id)
    {
        var response = await Client.GetAsync($"/api/fundos/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : $"Fundo {id} não encontrado.";
    }

    [McpServerTool(Name = "get_fund_performance"), Description("Retorna o histórico mensal de cota e patrimônio líquido de um fundo (últimos meses).")]
    public async Task<string> GetFundPerformance([Description("Id do fundo")] int id)
    {
        var response = await Client.GetAsync($"/api/fundos/{id}/historico");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : $"Fundo {id} não encontrado.";
    }

    [McpServerTool(Name = "list_fund_types"), Description("Lista os tipos de fundo (classificação ANBIMA/CVM: Renda Fixa, Ações, Multimercado, Cambial, FIDC, FII).")]
    public async Task<string> ListFundTypes() => await Client.GetStringAsync("/api/fundos/tipos");

    [McpServerTool(Name = "list_market_types"), Description("Lista os tipos de mercado de um fundo: Onshore (Brasil/CVM) ou Offshore (exterior).")]
    public async Task<string> ListMarketTypes() => await Client.GetStringAsync("/api/fundos/mercados");

    [McpServerTool(Name = "list_fund_managers"), Description("Lista as gestoras de recursos.")]
    public async Task<string> ListFundManagers() => await Client.GetStringAsync("/api/fundos/gestoras");

    [McpServerTool(Name = "list_fund_administrators"), Description("Lista as administradoras fiduciárias de fundos.")]
    public async Task<string> ListFundAdministrators() => await Client.GetStringAsync("/api/fundos/administradoras");

    [McpServerTool(Name = "find_fund"), Description("Acha fundos de investimento por nome ou CNPJ, num índice amplo (~34 mil fundos reais registrados na CVM) — cobre bem mais fundos do que list_funds, mas sem histórico de cota associado.")]
    public async Task<string> FindFund([Description("Nome (ou parte dele) ou CNPJ do fundo")] string query)
    {
        var response = await Client.GetAsync($"/api/fundos/buscar?q={Uri.EscapeDataString(query)}");
        if (!response.IsSuccessStatusCode) return $"Busca inválida: \"{query}\" (informe pelo menos 2 caracteres).";
        var body = await response.Content.ReadAsStringAsync();
        return body == "[]" ? $"Nenhum fundo encontrado para \"{query}\"." : body;
    }
}
