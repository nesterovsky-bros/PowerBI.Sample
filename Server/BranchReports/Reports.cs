using System.Collections.Immutable;

using NesterovskyBros.Utils;

using static NesterovskyBros.Utils.Extensions;

namespace Parser.Reports.Branch;

public class Reports
{
  public static readonly IDictionary<int, IReport<object>> Handlers =
    new IReport<object>[]
    {
      new Report1203001()
    }.
    ToImmutableDictionary(report => report.ReportNumber);

  /// <summary>
  /// A default report handler.
  /// </summary>
  /// <param name="items">Report pages.</param>
  /// <param name="tracer">Optional tracer instance.</param>
  /// <returns>Enumerable produced by parsing report.</returns>
  public static IEnumerable<object> DefaultHandler(
    IEnumerable<Page> items,
    ITracer? tracer) =>
    Handlers.TryGetValue(items.First().Report, out var handler) ?
    handler.Parse(items, tracer) :
    Array.Empty<object>();

  /// <summary>
  /// Parses the report.
  /// </summary>
  /// <param name="lines">Input rows.</param>
  /// <param name="tracer">Optional tracer instnance.</param>
  /// <param name="handler">A report handler.</param>
  /// <returns>Enumerable produced by parsing report.</returns>
  public static IEnumerable<object> Parse(
    IEnumerable<string> lines,
    ITracer? tracer = null,
    Func<IEnumerable<Page>, ITracer?, IEnumerable<object>>? handler = null) =>
    lines.
      Trace("Line", tracer).
      GroupAdjacent(startsAt: line => line.StartsWith("1")).
      Select(lines => lines.ToArray()).
      Select((lines, index) => new Page
      {
        PageNumber = index + 1,
        Report = Int(lines[1], 92, 7),
        CorrectnessDate = Date(lines[1], 16, 8),
        DestinationBranch = Int(lines[0], 34, 3),
        RecipientType = String(lines[0], 32, 1)!,
        RecipientNumber = Int(lines[0], 37, 2),
        Lines = lines
      }).
      Trace("Page", tracer).
      GroupAdjacent(page =>
      (
        page.Report,
        page.CorrectnessDate,
        page.DestinationBranch,
        page.RecipientType,
        page.RecipientNumber
      )).
      Trace("ReportPages", tracer).
      Select(pages => handler != null ?
        handler(pages, tracer) : DefaultHandler(pages, tracer)).
      Trace("Report", tracer).
      SelectMany(item => item);
}