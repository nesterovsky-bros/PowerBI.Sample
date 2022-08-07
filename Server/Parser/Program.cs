using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using NesterovskyBros.Parser;

using Parser.Reports;

var input = args[0];
var output = args[1];

var lineSource = new LineSource
{
  Path = input,
  Encoding = Encoding.UTF8,
  SkipTopEmptyLines = true
};

var handlers = 
  new IReport[]
  {
    new Report1203001()
  }.
  ToDictionary(report => report.ReportNumber);

IEnumerable<XElement> handler(IEnumerable<Page> items, ITracer? tracer) =>
  handlers.TryGetValue(items.First().report, out var handler) ?
    handler.Parse(items, tracer) :
    Array.Empty<XElement>();

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
    result.WriteTo(writer);
  }

  stopwatch.Stop();

  Console.WriteLine($"Execution time: {stopwatch.Elapsed}s.\n"); 
}

Console.WriteLine(
  "Name,Action,Count,Avg,Duration");

foreach(var item in 
  tracer.CollectedStatistics.Values.OrderByDescending(item => item.Duration))
{
  Console.WriteLine(
    $@"{item.Name},{item.Action},{item.Count},{
      (double)item.Duration / item.Count / 
        TimeSpan.TicksPerMillisecond :0.##},{
      (double)item.Duration / TimeSpan.TicksPerMillisecond}");
}
