using System.Globalization;

using NesterovskyBros.Utils;

namespace Parser.Reports.Branch;

public record Page
{
  public int PageNumber { get; init; }
  public int Report { get; init; }
  public DateTime CorrectnessDate { get; init; }
  public int DestinationBranch { get; init; }
  public string? RecipientType { get; init; }
  public int RecipientNumber { get; init; }
  public string[]? Lines { get; init; }
}

public interface IReport<out T>
{
  int ReportNumber { get; }

  IEnumerable<T> Parse(IEnumerable<Page> pages, ITracer? tracer);
}

public abstract class Report<T>: IReport<T>
{
  public virtual int ReportNumber { get; }
  public NumberFormatInfo? NumberFormat { get; init; }
  public string[]? DateFormats { get; init; }

  public abstract IEnumerable<T> Parse(
    IEnumerable<Page> pages,
    ITracer? tracer);

  public decimal Decimal(
    string? value,
    int start = 0,
    int length = int.MaxValue,
    NumberStyles numberStyles = NumberStyles.Currency) =>
    Extensions.Decimal(value, start, length, numberStyles, NumberFormat);

  public decimal? TryDecimal(
    string? value,
    int start = 0,
    int length = int.MaxValue,
    NumberStyles numberStyles = NumberStyles.Currency) =>
    Extensions.TryDecimal(value, start, length, numberStyles, NumberFormat);

  public DateTime Date(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    Extensions.Date(value, start, length, DateFormats);

  public DateTime? TryDate(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    Extensions.TryDate(value, start, length, DateFormats);
}
