using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

using REST.Models;

namespace REST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportersController : ControllerBase
{
  private readonly WideWorldImportersContext context;

  public ImportersController(WideWorldImportersContext context)
  {
    this.context = context;
  }

  [EnableQuery()]
  [HttpGet("Countries")]
  public IQueryable<Country> GetCountries()
  {
    return context.Countries;
  }
}
