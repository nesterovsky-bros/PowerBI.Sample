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

var statistics = tracer.Log.
  GroupBy(item => (item.Name, item.Action)).
  Select(group =>
  {
    var count = group.Count();
    
    var rescans = group.Key.Action != "GetEnumerator" ? 0 :
      group.
        GroupBy(item => item.ID).
        Select(group => group.Count() - 1).
        Sum();

    var avg = group.Average(item => item.Value);
    var avg2 = group.Average(item => (double)item.Value * item.Value);
    var stdev = Math.Sqrt(avg2 - avg * avg);

    return new
    {
      Name = group.Key.Name,
      Action = group.Key.Action,
      Duration = new TimeSpan(group.Sum(item => item.Duration)),
      Count = count,
      Rescans = rescans,
      Avg = avg,
      Stdev = stdev
    };
  }).
  OrderBy(item => (item.Name, item.Action)).
  ToArray();


Console.WriteLine(
  "Name,Action,Count,Rescans,Avg(Value),Stdev(Value),Duration");

foreach(var item in statistics)
{
  Console.WriteLine(
    $"{item.Name},{item.Action},{item.Count},{item.Rescans}," +
    $"{item.Avg:0.##},{item.Stdev:0.##},{item.Duration.TotalMilliseconds}");
}
