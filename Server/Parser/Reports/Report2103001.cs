namespace Parser.Reports;

using System.Globalization;
using System.Xml.Linq;

using NesterovskyBros.Parser;

using static NesterovskyBros.Parser.Functions;

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
            type: text.EndsWith("|ןח .סמ") ? "info" :
              text.EndsWith("| רוקמ") ? "transactions" :
              text.EndsWith(":תורעה") ? "comment" :
              text.EndsWith("|  עוציב .ת") ? "operation" : ""
          )).
          Where(item =>
            !item.text.StartsWith("0") &&
            !string.IsNullOrWhiteSpace(item.text))).
      SelectMany(item => item).
      Trace("ReportPages/ReportRow", tracer);

    var accounts = rows.
      GroupAdjacent(startsAt: row => row.text.
        Contains(" ------------------------------------------------------------------------------------------------------------------------------------")).
      Trace("ReportRow/AccountRows", tracer).
      Select(rows => rows.
        Skip(1).
        GroupAdjacent(startsAt: row => !string.IsNullOrEmpty(row.type)).
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
              case "info":
              {
                var row = group.ElementAt(1);
                var alertComment = NullIfEmpty(Bidi(Substring(row.text, 1, 65)));

                account = account with
                {
                  page = row.page,
                  row = row.row,
                  alertDate = DateTime.TryParseExact(
                      Substring(row.text, 67, 8),
                      "dd/MM/yy",
                      null,
                      DateTimeStyles.AllowWhiteSpaces, out var date) ?
                      date : null,
                  account = int.Parse(Substring(row.text, 127, 6)),
                  accountName = Bidi(Normalize(Substring(row.text, 86, 36))),
                  recipient = NullIfEmpty(Substring(row.text, 123, 3)),
                  alertType = int.Parse(Substring(row.text, 81, 4)),
                  idType = NullIfEmpty(Bidi(Substring(row.text, 76, 4))),
                  alertComment = alertComment
                };

                break;
              }
              case "transactions":
              {
                account = account with
                {
                  transactions = group.
                    Skip(2).
                    Where(row => !row.text.EndsWith(":טרופמ רואת  ")).
                    Select(row => new Transaction(
                      page: row.page,
                      row: row.row,
                      origin: NullIfEmpty(Bidi(Substring(row.text, 128, 5))),
                      description: NullIfEmpty(Bidi(Normalize(Substring(row.text, 113, 14)))),
                      date: DateTime.TryParseExact(
                        Substring(row.text, 102, 10),
                        "dd/MM/yyyy",
                        null,
                        DateTimeStyles.AllowWhiteSpaces,
                        out var date) ?
                        date : null,
                      actionType: int.TryParse(
                        Substring(row.text, 97, 4), 
                        out var actionType) ? 
                        actionType : null,
                      activityType: int.TryParse(
                        Substring(row.text, 91, 5), 
                        out var activityType) ? 
                        activityType : null,
                      reference: long.TryParse(
                        Substring(row.text, 77, 13), 
                        out var reference) ? 
                        reference : null,
                      currency: NullIfEmpty(Bidi(Normalize(Substring(row.text, 72, 4)))),
                      amount: decimal.TryParse(
                        Substring(row.text, 51, 20), 
                        NumberStyles.Currency, 
                        Reports.NumberFormat, 
                        out var amount) ? 
                        amount : null,
                      shelelAmount: decimal.TryParse(
                        Substring(row.text, 30, 20),
                        NumberStyles.Currency,
                        Reports.NumberFormat,
                        out var shekelAmount) ?
                        shekelAmount : null,
                      employeeID: long.TryParse(
                        Substring(row.text, 13, 17),
                        out var employeeID) ?
                        employeeID : null,
                      employeeCode: int.TryParse(
                        Substring(row.text, 7, 5),
                        out var employeeCode) ?
                        employeeCode : null,
                      station: int.TryParse(
                        Substring(row.text, 1, 4),
                        out var station) ?
                        station : null
                    )).
                    Trace("Section/Transactions", tracer).
                    ToArray()
                };

                break;
              }
              case "comment":
              {
                account = account with
                {
                  comment = NullIfEmpty(Bidi(Normalize(Substring(head.text, 1, 125))))
                };

                break;
              }
              case "operation":
              {
                account = account with
                {
                  operations = group.
                    Skip(2).
                    Select(row => new Operation(
                      page: row.page,
                      row: row.row,
                      date: DateTime.TryParseExact(
                        Substring(row.text, 123, 10),
                        "dd/MM/yyyy",
                        null,
                        DateTimeStyles.AllowWhiteSpaces,
                        out var date) ?
                        date : null,
                      cardName: NullIfEmpty(Bidi(Normalize(Substring(row.text, 97, 25)))),
                      clientID: long.TryParse(
                        Substring(row.text, 87, 9),
                        out var clientID) ?
                        clientID : null,
                      cardID: long.TryParse(
                        Substring(row.text, 75, 11),
                        out var cardID) ?
                        cardID : null,
                      comment: NullIfEmpty(Bidi(Normalize(Substring(row.text, 44, 30)))),
                      amount: decimal.TryParse(
                        Substring(row.text, 23, 20),
                        NumberStyles.Currency,
                        Reports.NumberFormat,
                        out var amount) ?
                        amount : null,
                      employeeID: long.TryParse(
                        Substring(row.text, 6, 16),
                        out var employeeID) ?
                        employeeID : null)).
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
