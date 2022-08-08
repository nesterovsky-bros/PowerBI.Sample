using System.Diagnostics;
using System.Text;
using System.Xml;

using NesterovskyBros.Collections;

using static NesterovskyBros.Collections.Functions;

using Parser.Reports;

var input = args[0];
var output = args[1];

var lineSource = new LineSource
{
  Path = input,
  Encoding = Encoding.UTF8,
  SkipTopEmptyLines = true
};

var xmlSettings = new XmlWriterSettings()
{
  Indent = true,
  OmitXmlDeclaration = true,
  ConformanceLevel = ConformanceLevel.Fragment
};

using var tracer = new Tracer();
var stopwatch = Stopwatch.StartNew();

using(var writer = XmlWriter.Create(output, xmlSettings))
{
  var results = Reports.Parse(lineSource.GetLines(), tracer).ToXml();

  foreach(var result in results)
  {
    result!.WriteTo(writer);
  }
}

stopwatch.Stop();

Console.WriteLine($"Execution time: {stopwatch.Elapsed}s.\n");

Console.WriteLine("Action,Count,Avg,Duration,Path");

foreach(var item in tracer.GetStatisticsByPath())
{
  Console.WriteLine(
    $@"{item.Action},{item.Count},{
      (double)item.Duration / item.Count / 
        TimeSpan.TicksPerMillisecond :0.##},{
      (double)item.Duration / TimeSpan.TicksPerMillisecond:0.##},{item.Name}");
}
