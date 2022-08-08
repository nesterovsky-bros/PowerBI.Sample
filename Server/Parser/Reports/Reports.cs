using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

using NesterovskyBros.Bidi;
using NesterovskyBros.Collections;

namespace Parser.Reports;

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

public static class Reports
{
  public static readonly NumberFormatInfo NumberFormat;

  public static readonly IDictionary<int, IReport> Handlers = 
    new IReport[]
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
      Trace("/Line", tracer).
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
      Trace("Line/Page", tracer).
      GroupAdjacent(page =>
      (
        page.Report,
        page.CorrectnessDate,
        page.DestinationBranch,
        page.RecipientType,
        page.RecipientNumber
      )).
      Trace("Page/ReportPages", tracer).
      SelectMany(pages => handler != null ? 
        handler(pages, tracer) : DefaultHandler(pages, tracer));

  public static string Substring(string? value, int start, int length)
  {
    if (value == null)
    {
      return "";
    }

    if (start < 0)
    {
      length += start;
      start = 0;
    }

    if (start >= value.Length)
    {
      return "";
    }

    if (start + length > value.Length)
    {
      length = value.Length - start;
    }

    if (length <= 0)
    {
      return "";
    }

    return value.Substring(start, length);
  }

  public static string? String(
    string? value,
    int start = 0,
    int length = int.MaxValue,
    bool normalize = true,
    bool trim = true,
    bool bidi = false,
    bool nullIfEmpty = true)
  {
    value = Substring(value, start, length);

    if (normalize)
    {
      value = Normalize(value);
    }
    else if (trim)
    {
      value = value.Trim();
    }
    // No more cases.

    if (nullIfEmpty && string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    if (bidi)
    {
      value = Bidi(value);
    }

    return value;
  }

  public static int Int(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    int.Parse(Substring(value, start, length));

  public static int? TryInt(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    int.TryParse(
      Substring(value, start, length), 
      out var result) ? result : null;

  public static long Long(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    long.Parse(Substring(value, start, length));

  public static long? TryLong(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    long.TryParse(
      Substring(value, start, length), 
      out var result) ? result : null;

  public static decimal Decimal(
    string? value,
    int start = 0,
    int length = int.MaxValue,
    NumberStyles numberStyles = NumberStyles.Currency,
    IFormatProvider? formatProvider = null) =>
    decimal.Parse(
      Substring(value, start, length), 
      numberStyles, 
      formatProvider ?? NumberFormat);

  public static decimal? TryDecimal(
    string? value,
    int start = 0,
    int length = int.MaxValue,
    NumberStyles numberStyles = NumberStyles.Currency,
    IFormatProvider? formatProvider = null) =>
    decimal.TryParse(
      Substring(value, start, length),
      numberStyles,
      formatProvider ?? NumberFormat,
      out var result) ? result : null;

  public static DateTime Date(
    string? value,
    int start = 0,
    int length = int.MaxValue,
    string? format = null)
  {
    value = Substring(value, start, length);

    return format == null ?
      DateTime.ParseExact(
        value, 
        dateFormats, 
        null, 
        DateTimeStyles.AllowWhiteSpaces) :
      DateTime.ParseExact(
        value, 
        format, 
        null, 
        DateTimeStyles.AllowWhiteSpaces);
  }

  public static DateTime? TryDate(
    string? value,
    int start = 0,
    int length = int.MaxValue,
    string? format = null)
  {
    value = Substring(value, start, length);

    return format == null ?
      DateTime.TryParseExact(
        value,
        dateFormats,
        null,
        DateTimeStyles.AllowWhiteSpaces,
        out var result) ? result : null :
      DateTime.TryParseExact(
        value,
        format,
        null,
        DateTimeStyles.AllowWhiteSpaces,
        out result) ? result : null;
  }

  public static string? NullIfEmpty(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  public static string Normalize(string? value)
  {
    return value == null ? "" : spaces.Replace(value, " ").Trim();
  }

  public static string? Bidi(string? value) =>
    BidiConverter.Convert(value, true, false);

  private static readonly Regex spaces = new(@"\s\s+", RegexOptions.Compiled);
  private static readonly string[] dateFormats =
    new[] { "dd/MM/yyyy", "dd/MM/yy", "dd.MM.yyyy", "dd.MM.yy", "yyyy-MM-dd" };

  static Reports()
  {
    var numberFormat = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();

    numberFormat.CurrencyDecimalSeparator = ".";
    numberFormat.CurrencyGroupSeparator = ",";

    NumberFormat = NumberFormatInfo.ReadOnly(numberFormat);
  }
}