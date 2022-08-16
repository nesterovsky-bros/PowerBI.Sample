using System.Diagnostics;
using System.Text;
using System.Xml;

using NesterovskyBros.Utils;

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

var tracer = new Tracer();
var stopwatch = Stopwatch.StartNew();

using(var writer = XmlWriter.Create(output, xmlSettings))
{
  var results = Parser.Reports.Branch.Reports.
    Parse(lineSource.GetLines(), tracer).
    ToXml();

  foreach(var result in results)
  {
    result!.WriteTo(writer);
  }
}

stopwatch.Stop();

Console.WriteLine($"Execution time: {stopwatch.Elapsed}s.\n");

Console.WriteLine("Name,Caller,Count,DistinctCount,Avg,Duration,Actions");

foreach(var item in tracer.CollectedStatistics.Values.OrderBy(item => item.ID))
{
  Console.WriteLine(
    $@"{item.Name},{item.Caller},{item.Count},{item.DistinctCount},{
      (double)item.Duration / item.Count / 
      TimeSpan.TicksPerMillisecond :0.##},{
      (double)item.Duration / TimeSpan.TicksPerMillisecond:0.##},{
      string.Join(',', item.Actions.OrderBy(item => item))}");
}
