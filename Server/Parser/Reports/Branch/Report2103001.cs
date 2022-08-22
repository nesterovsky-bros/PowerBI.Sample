using NesterovskyBros.Utils;

using static NesterovskyBros.Utils.Extensions;

namespace Parser.Reports.Branch;

public class Report1203001: Report<Report1203001.Account>
{
  public record Account
  {
    public int Report { get; init; }
    public DateTime CorrectnessDate { get; init; }
    public int DestinationBranch { get; init; }
    public string? RecipientType { get; init; }
    public int RecipientNumber { get; init; }
    public int Page { get; init; }
    public int Row { get; init; }
    public DateTime? AlertDate { get; init; }
    public int AccountNumber { get; init; }
    public string? AccountName { get; init; }
    public string? Recipient { get; init; }
    public int AlertType { get; init; }
    public string? IdType { get; init; }
    public string? AlertComment { get; init; }
    public string? Comment { get; init; }
    public Transaction[]? Transactions { get; init; }
    public Operation[]? Operations { get; init; }
  };

  public record Transaction
  {
    public int Page { get; init; }
    public int Row { get; init; }
    public string? Origin { get; init; }
    public string? Description { get; init; }
    public DateTime? Date { get; init; }
    public int? ActionType { get; init; }
    public int? ActivityType { get; init; }
    public long? Reference { get; init; }
    public string? Currency { get; init; }
    public decimal? Amount { get; init; }
    public decimal? ShelelAmount { get; init; }
    public long? EmployeeID { get; init; }
    public int? EmployeeCode { get; init; }
    public int? Station { get; init; }
  }

  public record Operation
  {
    public int Page { get; init; }
    public int Row { get; init; }
    public DateTime? Date { get; init; }
    public string? CardName { get; init; }
    public long? ClientID { get; init; }
    public long? CardID { get; init; }
    public string? Comment { get; init; }
    public decimal? Amount { get; init; }
    public long? EmployeeID { get; init; }
  }

  public override int ReportNumber => 1203001;

  public override IEnumerable<Account> Parse(
    IEnumerable<Page> pages,
    ITracer? tracer) =>
    pages.
      Where(page => page.Lines.Length > 8 &&
        !page.Lines[4].
          Contains("*****     ! ! ! ! !   ה ז   ף י נ ס ל   ם ו י ה   ת ו ע ו נ ת   ן י א     *****")).
      SelectMany(page => page.Lines.
        SkipLast(5).
        Skip(3).
        Select((text, index) =>
        (
          page,
          row: index + 4,
          text,
          type: text.StartsWith("0") || 
            string.IsNullOrWhiteSpace(text) ? 'E' : // Empty
            text.EndsWith("|ןח .סמ") ? 'I' : // Info
            text.EndsWith("| רוקמ") ? 'T' : // Transaction
            text.EndsWith(":תורעה") ? 'C' : // Comment
            text.EndsWith("|  עוציב .ת") ? 'O' : ' ' // Operation; Other
        )).
        Where(item => item.type != 'E')).
      Trace("ReportRow", tracer).
      GroupAdjacent(startsAt: row => row.text.
        Contains(" ------------------------------------------------------------------------------------------------------------------------------------")).
      Trace("AccountRows", tracer).
      Select(rows => rows.
        Skip(1).
        GroupAdjacent(startsAt: row => row.type != ' ').
        Trace("Section", tracer).
        Aggregate(
          new Account(),
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
                  Report = row.page.Report,
                  CorrectnessDate = row.page.CorrectnessDate,
                  DestinationBranch = row.page.DestinationBranch,
                  RecipientType = row.page.RecipientType,
                  RecipientNumber = row.page.RecipientNumber,
                  Page = row.page.PageNumber,
                  Row = row.row,
                  AlertDate = TryDate(row.text, 67, 8),
                  AccountNumber = Int(row.text, 127, 6),
                  AccountName = String(row.text, 86, 36, bidi: true),
                  Recipient = String(row.text, 123, 3),
                  AlertType = Int(row.text, 81, 4),
                  IdType = String(row.text, 76, 4, bidi: true),
                  AlertComment = String(row.text, 1, 65, bidi: true)
                };

                break;
              }
              case 'T': // Transactions
              {
                account = account with
                {
                  Transactions = group.
                    Skip(2).
                    Where(row => !row.text.EndsWith(":טרופמ רואת  ")).
                    Select(row => new Transaction
                    {
                      Page = row.page.PageNumber,
                      Row = row.row,
                      Origin = String(row.text, 128, 5, bidi: true),
                      Description = String(row.text, 113, 14, bidi: true),
                      Date = TryDate(row.text, 102, 10),
                      ActionType = TryInt(row.text, 97, 4),
                      ActivityType = TryInt(row.text, 91, 5),
                      Reference = TryLong(row.text, 77, 13),
                      Currency = String(row.text, 72, 4, bidi: true),
                      Amount = TryDecimal(row.text, 51, 20),
                      ShelelAmount = TryDecimal(row.text, 30, 20),
                      EmployeeID = TryLong(row.text, 13, 17),
                      EmployeeCode = TryInt(row.text, 7, 5),
                      Station = TryInt(row.text, 1, 4)
                    }).
                    Trace("Transactions", tracer).
                    ToArray()
                };

                break;
              }
              case 'C': // Comment
              {
                account = account with
                {
                  Comment = String(head.text, 1, 125, bidi: true)
                };

                break;
              }
              case 'O': // Operations
              {
                account = account with
                {
                  Operations = group.
                    Skip(2).
                    Select(row => new Operation
                    {
                      Page = row.page.PageNumber,
                      Row = row.row,
                      Date = TryDate(row.text, 123, 10),
                      CardName = String(row.text, 97, 25, bidi: true),
                      ClientID = TryLong(row.text, 87, 9),
                      CardID = TryLong(row.text, 75, 11),
                      Comment = String(row.text, 44, 30, bidi: true),
                      Amount = TryDecimal(row.text, 23, 20),
                      EmployeeID = TryLong(row.text, 6, 16)
                    }).
                    Trace("Operations", tracer).
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
      Trace("Account", tracer);
}
