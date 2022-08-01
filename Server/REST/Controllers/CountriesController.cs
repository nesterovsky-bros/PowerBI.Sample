using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

using REST.Models;

namespace REST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CountriesController: ODataController
{
  private readonly WideWorldImportersContext context;

  public CountriesController(WideWorldImportersContext context)
  {
    this.context = context;
  }

  [EnableQuery()]
  [HttpGet]
  public ActionResult<IQueryable<Country>> Get()
  {
    return context.Countries;
  }
}
