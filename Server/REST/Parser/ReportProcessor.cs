using System.Xml.Linq;

namespace REST.Parser;

using static Functions;

public record Page(int page, string[] lines);

public record ReportLine(int page, int row, string text);

public record ReportData(int report, Page head, IEnumerable<ReportLine> lines);

public class ReportProcessor
{
  public static IEnumerable<XElement> Parse(
    IEnumerable<string> lines,
    Func<ReportData, IEnumerable<XElement>> reportHandler)
  {
    var pages = lines.
      // Form groups of lines.
      // GroupAdjacent(this IEnumeration<T>) => IEnumeration<(T head, IEnumeration<T> items)>
      // Note: group items must be consumed before processing next group.
      GroupAdjacent(startsAt: line => line.StartsWith("1")).
      // Create enumeration of pages.
      Select((group, index) =>
        new Page(page: index + 1, lines: group.items.ToArray()));

    var reports = pages.
      // Form enumeration of tuples (int report, Page page)
      Select(page => (report: int.Parse(Substring(page.lines[0], 70, 7)), page)).
      // For groups of pages per report.
      GroupAdjacent(item => item.report).
      // Create a enumeration of ReportData.
      Select(group => new ReportData(
        // Report number.
        report: group.head.report,
        // First page of the report.
        head: group.head.page,
        // Enumeration of ReportLine - all lines of report without page headers.
        lines: group.items.SelectMany(item =>
          item.page.lines.Skip(2).Select((line, index) =>
            new ReportLine(
              page: item.page.page,
              row: index + 3,
              text: line)))));

    return reports.
      // Process report data with specific handler.
      Select(reportHandler).
      // produce final enumeration of XElement(s).
      SelectMany(item => item);
  }
}