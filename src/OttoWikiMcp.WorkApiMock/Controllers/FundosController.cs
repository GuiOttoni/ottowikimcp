using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace OttoWikiMcp.WorkApiMock.Controllers;

[ApiController]
[Route("api/fundos")]
public sealed class FundosController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Fundo>> List([FromQuery] int? tipoDeFundoId, [FromQuery] int? tipoMercadoId)
    {
        var query = FundosData.Fundos.AsEnumerable();
        if (tipoDeFundoId is not null) query = query.Where(f => f.TipoDeFundoId == tipoDeFundoId);
        if (tipoMercadoId is not null) query = query.Where(f => f.TipoMercadoId == tipoMercadoId);
        return Ok(query.OrderByDescending(f => f.PatrimonioLiquido));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Fundo> GetById(int id)
    {
        var fundo = FundosData.Fundos.FirstOrDefault(f => f.Id == id);
        return fundo is null ? NotFound() : Ok(fundo);
    }

    [HttpGet("{id:int}/historico")]
    public ActionResult<IEnumerable<CotaHistorico>> Historico(int id)
    {
        if (FundosData.Fundos.All(f => f.Id != id)) return NotFound();
        return Ok(FundosData.HistoricoCotas.Where(h => h.FundoId == id).OrderBy(h => h.Data));
    }

    [HttpGet("tipos")]
    public ActionResult<IEnumerable<TipoDeFundo>> TiposDeFundo() => Ok(FundosData.TiposDeFundo);

    [HttpGet("mercados")]
    public ActionResult<IEnumerable<TipoMercado>> TiposMercado() => Ok(FundosData.TiposMercado);

    /// <summary>Lista unificada de instituições (administradoras e/ou gestoras — ver <see cref="Instituicao.Papeis"/>).</summary>
    [HttpGet("instituicoes")]
    public ActionResult<IEnumerable<Instituicao>> Instituicoes([FromQuery] string? papel)
    {
        var query = FundosData.Instituicoes.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(papel))
            query = query.Where(i => i.Papeis.Contains(papel, StringComparer.OrdinalIgnoreCase));
        return Ok(query.OrderBy(i => i.Nome));
    }

    [HttpGet("instituicoes/{id:int}")]
    public ActionResult<Instituicao> GetInstituicao(int id)
    {
        var inst = FundosData.Instituicoes.FirstOrDefault(i => i.Id == id);
        return inst is null ? NotFound() : Ok(inst);
    }

    /// <summary>Atalho pra instituições com papel "Gestora" — mesma tabela unificada, só filtrada.</summary>
    [HttpGet("gestoras")]
    public ActionResult<IEnumerable<Instituicao>> Gestoras() =>
        Ok(FundosData.Instituicoes.Where(i => i.Papeis.Contains("Gestora")).OrderBy(i => i.Nome));

    /// <summary>Atalho pra instituições com papel "Administradora" — mesma tabela unificada, só filtrada.</summary>
    [HttpGet("administradoras")]
    public ActionResult<IEnumerable<Instituicao>> Administradoras() =>
        Ok(FundosData.Instituicoes.Where(i => i.Papeis.Contains("Administradora")).OrderBy(i => i.Nome));

    /// <summary>
    /// Busca por nome ou CNPJ num índice bem maior (~34 mil fundos registrados na CVM,
    /// <c>Data/fundos-search-index.json</c>) do que o conjunto "curado" com histórico de cota —
    /// cobre praticamente qualquer fundo em funcionamento normal registrado na CVM, não só os
    /// que este mock tem em detalhe.
    /// </summary>
    [HttpGet("buscar")]
    public ActionResult<IEnumerable<FundoBusca>> Buscar([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest(new { error = "Informe pelo menos 2 caracteres (nome ou CNPJ)." });

        var termo = q.Trim();
        var digitsOnly = new string(termo.Where(char.IsDigit).ToArray());
        var buscaPorCnpj = digitsOnly.Length >= 4;

        var resultados = FundosData.IndiceBusca.Where(f =>
            f.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
            (buscaPorCnpj && f.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "").Contains(digitsOnly)));

        return Ok(resultados.Take(50));
    }

    /// <summary>
    /// Consulta AO VIVO (não "baked") o CNPJ na BrasilAPI — usado quando o usuário quer dado
    /// cadastral fresco de uma instituição específica, em vez do snapshot de <see cref="Instituicoes"/>.
    /// Único ponto deste serviço que chama uma API pública em tempo real por request.
    /// </summary>
    [HttpGet("buscar-cnpj/{cnpj}")]
    public async Task<IActionResult> BuscarCnpj(string cnpj)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14) return BadRequest(new { error = "CNPJ inválido — informe 14 dígitos." });

        var client = httpClientFactory.CreateClient("BrasilApi");
        var response = await client.GetAsync($"/api/cnpj/v1/{digits}");
        if (!response.IsSuccessStatusCode)
            return NotFound(new { error = $"CNPJ não encontrado na BrasilAPI (HTTP {(int)response.StatusCode})." });

        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }
}
