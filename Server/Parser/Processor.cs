using System.Xml.Linq;

namespace NesterovskyBros.Parser;

using static Functions;

public record Page(
  int page,
  int report,
  DateTime correctnessDate,
  int destinationBranch,
  string recipientType,
  int recipientNumber,
  string[] lines);

public class Processor
{
  public static IEnumerable<XElement> Parse(
    IEnumerable<string> lines,
    Func<IEnumerable<Page>, ITracer?, IEnumerable<XElement?>> handler,
    ITracer? tracer = null)
  {
    var pages = lines.
      Trace("/Line", tracer).
      // Form groups of lines.
      // Note: group items should not be enumerated after
      //       processing of the next group.
      //       group items may be consumed at most one
      //       time except first item, which is cached and may
      //       be requested multiple times, e.g with items.First() .
      GroupAdjacent(startsAt: line => line.StartsWith("1")).
      Select(items => items.ToArray()).
      Select((lines, index) => new Page(
        page: index + 1,
        report: int.Parse(Substring(lines[1], 92, 7)),
        correctnessDate: DateTime.ParseExact(Substring(lines[1], 16, 8), "dd.MM.yy", null),
        destinationBranch: int.Parse(Substring(lines[0], 34, 3)),
        recipientType: Substring(lines[0], 32, 1),
        recipientNumber: int.Parse(Substring(lines[0], 37, 2)),
        lines: lines)).
      Trace("Line/Page", tracer);

    var reports = pages.
      GroupAdjacent(item => 
      (
        item.report, 
        item.correctnessDate, 
        item.destinationBranch, 
        item.recipientType, 
        item.recipientNumber
      )).
      Trace("Page/ReportPages", tracer);

    return reports.
      // Process report data with specific handler.
      Select(report => handler(report, tracer)).
      // produce final enumeration of XElement(s).
      SelectMany(item => item).
      Where(item => item != null) as IEnumerable<XElement>;
  }
}