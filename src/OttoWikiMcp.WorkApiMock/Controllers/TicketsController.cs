using Microsoft.AspNetCore.Mvc;

namespace OttoWikiMcp.WorkApiMock.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Ticket>> List([FromQuery] TicketStatus? status, [FromQuery] int? instituicaoId)
    {
        var query = FakeData.Tickets.AsEnumerable();
        if (status is not null) query = query.Where(t => t.Status == status);
        if (instituicaoId is not null) query = query.Where(t => t.InstituicaoId == instituicaoId);
        return Ok(query.OrderByDescending(t => t.CreatedAt));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Ticket> GetById(int id)
    {
        var ticket = FakeData.Tickets.FirstOrDefault(t => t.Id == id);
        return ticket is null ? NotFound() : Ok(ticket);
    }
}
