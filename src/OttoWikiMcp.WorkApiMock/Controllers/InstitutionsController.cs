using Microsoft.AspNetCore.Mvc;

namespace OttoWikiMcp.WorkApiMock.Controllers;

[ApiController]
[Route("api/institutions")]
public sealed class InstitutionsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Institution>> List() => Ok(FakeData.Institutions);

    [HttpGet("{id:int}")]
    public ActionResult<Institution> GetById(int id)
    {
        var institution = FakeData.Institutions.FirstOrDefault(i => i.Id == id);
        return institution is null ? NotFound() : Ok(institution);
    }
}
