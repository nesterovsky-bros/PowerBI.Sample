using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

using REST.Models;

namespace REST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StatusController: ODataController
{
  private readonly WideWorldImportersContext context;

  public StatusController(WideWorldImportersContext context)
  {
    this.context = context;
  }

  [EnableQuery()]
  [HttpGet]
  public IQueryable<Status> Get()
  {
    return context.Statuses;
  }

  [HttpPost]
  public async Task<Status> Set([FromBody] Status status)
  {
    if (await context.Statuses.AnyAsync(item => item.ID == status.ID))
    {
      context.Update(status);
    }
    else
    {
      await context.Statuses.AddAsync(status);
    }

    await context.SaveChangesAsync();

    return status;
  }
}
