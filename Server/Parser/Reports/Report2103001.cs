namespace Parser.Reports;

using System.Globalization;
using System.Xml.Linq;

using NesterovskyBros.Parser;

using static NesterovskyBros.Parser.Functions;

public class Report1203001: IReport
{
  public record Transaction(int page, int row, string text);

  public record Account(
    int page,
    int row,
    DateTime? alertDate,
    int account,
    string? accountName,
    string? recipient,
    int alertType,
    string? idType,
    string? alertComment,
    IEnumerable<Transaction> transactions);

  public record Report(
    int report,
    DateTime correctnessDate,
    int destinationBranch,
    string recipientType,
    int recipientNumber,
    IEnumerable<Account> accounts);

  public int ReportNumber => 1203001;

  public IEnumerable<XElement> Parse(IEnumerable<Page> items, ITracer? tracer)
  {
    var page = items.First();

    var rows = items.
      Where(page =>
        page.lines.Length > 5 &&
        !page.lines[4].
          Contains("*****     ! ! ! ! !   ה ז   ף י נ ס ל   ם ו י ה   ת ו ע ו נ ת   ן י א     *****")).
      Select(page =>
        page.lines.
          SkipLast(5).
          Skip(3).
          Select((text, index) => (row: index + 4, text)).
          Where(item =>
            !item.text.StartsWith("0") &&
            !string.IsNullOrWhiteSpace(item.text)).
          Select(item => (page.page, item.row, item.text))).
      SelectMany(item => item).
      Trace("ReportPage/Row", tracer);

    var accounts = rows.
      GroupAdjacent(startsAt: row => row.text.
        Contains(" ------------------------------------------------------------------------------------------------------------------------------------")).
      Select(rows =>
        rows.
          Where((row, index) => index switch
          {
            0 or 1 or 3 or 4 or 5 => false,
            _ => true
          })).
      Select(rows =>
      {
        var head = rows.First();
        var alertComment = NullIfEmpty(Bidi(Substring(head.text, 1, 65)));

        return new Account(
          page: head.page,
          row: head.row,
          alertDate: DateTime.TryParseExact(
            Substring(head.text, 67, 8), 
            "dd/MM/yy", 
            null, 
            DateTimeStyles.AllowWhiteSpaces, out var date) ? 
            date : null,
          account: int.Parse(Substring(head.text, 127, 6)),
          accountName: Bidi(Normalize(Substring(head.text, 86, 36))),
          recipient: NullIfEmpty(Substring(head.text, 123, 3)),
          alertType: int.Parse(Substring(head.text, 81, 4)),
          idType: NullIfEmpty(Bidi(Substring(head.text, 76, 4))),
          alertComment,
          transactions: rows.
            Skip(1).Select(row => new Transaction(
              page: row.page,
              row: row.row,
              text: row.text)));
      }).
      Trace("Row/Section", tracer);

    var report = new Report(
      report: page.report,
      correctnessDate: page.correctnessDate,
      destinationBranch: page.destinationBranch,
      recipientType: page.recipientType,
      recipientNumber: page.recipientNumber,
      accounts);

    if (!accounts.Any())
    {
      return Array.Empty<XElement>();
    }

    return new[] { ToXml(report, "report")! };
  }
}
