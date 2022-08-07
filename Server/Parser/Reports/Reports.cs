using System.Collections.Immutable;
using System.Globalization;

namespace Parser.Reports;

public record Page(
  int page,
  int report,
  DateTime correctnessDate,
  int destinationBranch,
  string recipientType,
  int recipientNumber,
  string[] lines);

public static class Reports
{
  public static readonly NumberFormatInfo NumberFormat;

  public static readonly IDictionary<int, IReport> Handlers = 
    new IReport[]
    {
      new Report1203001()
    }.
    ToImmutableDictionary(report => report.ReportNumber);

  static Reports()
  {
    var numberFormat = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();

    numberFormat.CurrencyDecimalSeparator = ".";
    numberFormat.CurrencyGroupSeparator = ",";

    NumberFormat = NumberFormatInfo.ReadOnly(numberFormat);
  }

}