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
        [Description("Id da instituição (opcional)")] int? institutionId = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        if (institutionId is not null) query.Add($"institutionId={institutionId}");
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

    [McpServerTool(Name = "list_institutions"), Description("Lista todas as instituições clientes.")]
    public async Task<string> ListInstitutions() => await Client.GetStringAsync("/api/institutions");

    [McpServerTool(Name = "get_institution"), Description("Retorna uma instituição específica pelo id.")]
    public async Task<string> GetInstitution([Description("Id da instituição")] int id)
    {
        var response = await Client.GetAsync($"/api/institutions/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : $"Instituição {id} não encontrada.";
    }
}
