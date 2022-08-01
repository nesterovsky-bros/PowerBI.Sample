using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData;

using REST.Models;
using NetTopologySuite.IO.Converters;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;
var configuration = builder.Configuration;

services.
  AddControllers().
  AddJsonOptions(options => 
  {
    options.JsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
  }).
  AddOData(options =>
  {
    options.
      Select().
      OrderBy().
      Count().
      Expand().
      Filter().
      SkipToken().
      SetMaxTop(null);//.
      //AddRouteComponents("api", GetEdmModel());
  });

services.AddCors(options =>
{
  options.AddDefaultPolicy(
      policy =>
      {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
      });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();


services.AddDbContext<WideWorldImportersContext>(
  options =>
  {
    options.UseSqlServer(
      configuration.GetConnectionString("WideWorldImporters"),
      optionsBuilder => optionsBuilder.UseNetTopologySuite());
  });

builder.Services.AddResponseCompression(options =>
{
  options.EnableForHttps = true;
});

var app = builder.Build();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();

//IEdmModel GetEdmModel()
//{
//  var builder = new ODataConventionModelBuilder();

//  builder.EntitySet<Invoice>("Invoices");

//  return builder.GetEdmModel();
//}
