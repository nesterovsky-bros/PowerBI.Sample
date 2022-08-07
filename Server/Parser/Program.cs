using System.Diagnostics;
using System.Text;
using System.Xml;

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

using var tracer = new Tracer();

{
  var stopwatch = Stopwatch.StartNew();
  var results = Reports.Parse(lineSource.GetLines(), tracer);

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
