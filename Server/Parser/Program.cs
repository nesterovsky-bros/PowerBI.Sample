﻿using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using NesterovskyBros.Parser;

using static NesterovskyBros.Parser.Functions;

using Parser.Reports;

var input = args[0];
var output = args[1];

var lineSource = new LineSource
{
  Path = input,
  Encoding = Encoding.UTF8,
  SkipTopEmptyLines = true
};

IEnumerable<object?> handler(IEnumerable<Page> items, ITracer? tracer) =>
  Reports.Handlers.TryGetValue(items.First().report, out var handler) ?
    handler.Parse(items, tracer) :
    Array.Empty<object?>();

using var tracer = new Tracer();

{
  var stopwatch = Stopwatch.StartNew();
  var results = Processor.Parse(lineSource.GetLines(), handler, tracer);

  using var writer = XmlWriter.Create(
    output,
    new()
    {
      Indent = true,
      OmitXmlDeclaration = true,
      ConformanceLevel = ConformanceLevel.Fragment
    });

  foreach(var result in results)
  {
    ToXml(result!)!.WriteTo(writer);
  }

  stopwatch.Stop();

  Console.WriteLine($"Execution time: {stopwatch.Elapsed}s.\n"); 
}

Console.WriteLine(
  "Action,Count,Avg,Duration,Path");

foreach(var item in tracer.GetStatisticsByPath())
{
  Console.WriteLine(
    $@"{item.Action},{item.Count},{
      (double)item.Duration / item.Count / 
        TimeSpan.TicksPerMillisecond :0.##},{
      (double)item.Duration / TimeSpan.TicksPerMillisecond:0.##},{item.Name}");
}
