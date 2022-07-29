using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData;

using REST.Models;
using NetTopologySuite.IO.Converters;

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
      SetMaxTop(null);
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


app.UseAuthorization();

app.MapControllers();

app.Run();
