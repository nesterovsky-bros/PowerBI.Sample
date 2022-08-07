namespace Parser.Reports;

using NesterovskyBros.Parser;

using static Parser.Reports.Reports;

public class Report1203001: IReport
{
  public record Transaction(
    int page, 
    int row, 
    string? origin,
    string? description,
    DateTime? date,
    int? actionType,
    int? activityType,
    long? reference,
    string? currency,
    decimal? amount,
    decimal? shelelAmount,
    long? employeeID,
    int? employeeCode,
    int? station);

  public record Operation(
    int page,
    int row,
    DateTime? date,
    string? cardName,
    long? clientID,
    long? cardID,
    string? comment,
    decimal? amount,
    long? employeeID);

  public record Account(
    int report,
    DateTime correctnessDate,
    int destinationBranch,
    string recipientType,
    int recipientNumber,

    int page = 0,
    int row = 0,
    DateTime? alertDate = null,
    int account = 0,
    string? accountName = null,
    string? recipient = null,
    int alertType = 0,
    string? idType = null,
    string? alertComment = null,
    string? comment = null,
    IEnumerable<Transaction>? transactions = null,
    IEnumerable<Operation>? operations = null);

  public int ReportNumber => 1203001;

  IEnumerable<object?> IReport.Parse(
    IEnumerable<Page> items, 
    ITracer? tracer) => Parse(items, tracer);

  public IEnumerable<Account?> Parse(IEnumerable<Page> items, ITracer? tracer)
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
          Select((text, index) => 
          (
            page: page.page,
            row: index + 4, 
            text, 
            type: 
              // Empty
              text.StartsWith("0") || string.IsNullOrWhiteSpace(text) ? 'E' :
              text.EndsWith("|ןח .סמ") ? 'I' : // Info
              text.EndsWith("| רוקמ") ? 'T' : // Transaction
              text.EndsWith(":תורעה") ? 'C' : // Comment
              text.EndsWith("|  עוציב .ת") ? 'O' : ' ' // Operation; other
          )).
          Where(item => item.type != 'E')).
      SelectMany(item => item).
      Trace("ReportPages/ReportRow", tracer);

    var accounts = rows.
      GroupAdjacent(startsAt: row => row.text.
        Contains(" ------------------------------------------------------------------------------------------------------------------------------------")).
      Trace("ReportRow/AccountRows", tracer).
      Select(rows => rows.
        Skip(1).
        GroupAdjacent(startsAt: row => row.type != ' ').
        Trace("AccountRows/Section", tracer).
        Aggregate(
          new Account(
            report: page.report,
            correctnessDate: page.correctnessDate,
            destinationBranch: page.destinationBranch,
            recipientType: page.recipientType,
            recipientNumber: page.recipientNumber),
          (account, group) =>
          {
            var head = group.First();

            switch(head.type)
            {
              case 'I': // Info
              {
                var row = group.ElementAt(1);

                account = account with
                {
                  page = row.page,
                  row = row.row,
                  alertDate = TryDate(row.text, 67, 8),
                  account = Int(row.text, 127, 6),
                  accountName = String(row.text, 86, 36, bidi: true),
                  recipient = String(row.text, 123, 3),
                  alertType = Int(row.text, 81, 4),
                  idType = String(row.text, 76, 4, bidi: true),
                  alertComment = String(row.text, 1, 65, bidi: true)
                };

                break;
              }
              case 'T': // Transactions
              {
                account = account with
                {
                  transactions = group.
                    Skip(2).
                    Where(row => !row.text.EndsWith(":טרופמ רואת  ")).
                    Select(row => new Transaction(
                      page: row.page,
                      row: row.row,
                      origin: String(row.text, 128, 5, bidi: true),
                      description: String(row.text, 113, 14, bidi: true),
                      date: TryDate(row.text, 102, 10),
                      actionType: TryInt(row.text, 97, 4),
                      activityType: TryInt(row.text, 91, 5),
                      reference: TryLong(row.text, 77, 13),
                      currency: String(row.text, 72, 4, bidi: true),
                      amount: TryDecimal(row.text, 51, 20),
                      shelelAmount: TryDecimal(row.text, 30, 20),
                      employeeID: TryLong(row.text, 13, 17),
                      employeeCode: TryInt(row.text, 7, 5),
                      station: TryInt(row.text, 1, 4)
                    )).
                    Trace("Section/Transactions", tracer).
                    ToArray()
                };

                break;
              }
              case 'C': // Comment
              {
                account = account with
                {
                  comment = String(head.text, 1, 125, bidi: true)
                };

                break;
              }
              case 'O': // Operations
              {
                account = account with
                {
                  operations = group.
                    Skip(2).
                    Select(row => new Operation(
                      page: row.page,
                      row: row.row,
                      date: TryDate(row.text, 123, 10),
                      cardName: String(row.text, 97, 25, bidi: true),
                      clientID: TryLong(row.text, 87, 9),
                      cardID: TryLong(row.text, 75, 11),
                      comment: String(row.text, 44, 30, bidi: true),
                      amount: TryDecimal(row.text, 23, 20),
                      employeeID: TryLong(row.text, 6, 16))).
                    Trace("Section/Operations", tracer).
                    ToArray()
                };

                break;
              }
              default:
              {
                break;
              }
            }

            return account;
          })).
        Trace("Section/Account", tracer);

    return accounts;
  }
}
