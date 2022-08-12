using System.Globalization;
using System.Text.RegularExpressions;

using NesterovskyBros.Bidi;

namespace NesterovskyBros.Utils;

/// <summary>
/// Extension functions to simplify streaming processing.
/// </summary>
public static partial class Extensions
{
  public static readonly NumberFormatInfo NumberFormat;

  public static string Substring(string? value, int start, int length)
  {
    if(value == null)
    {
      return "";
    }

    if(start < 0)
    {
      length += start;
      start = 0;
    }

    if(start >= value.Length)
    {
      return "";
    }

    if(start + length > value.Length)
    {
      length = value.Length - start;
    }

    if(length <= 0)
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

    if(normalize)
    {
      value = Normalize(value);
    }
    else if(trim)
    {
      value = value.Trim();
    }
    // No more cases.

    if(nullIfEmpty && string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    if(bidi)
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
    params string[]? formats) =>
    DateTime.ParseExact(
      Substring(value, start, length),
      formats?.Length > 0 ? formats : dateFormats,
      null,
      DateTimeStyles.AllowWhiteSpaces);

  public static DateTime? TryDate(
    string? value,
    int start = 0,
    int length = int.MaxValue,
    params string[]? formats) =>
    DateTime.TryParseExact(
      Substring(value, start, length),
      formats?.Length > 0 ? formats : dateFormats,
      null,
      DateTimeStyles.AllowWhiteSpaces,
      out var result) ? result : null;

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

  static Extensions()
  {
    var numberFormat = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();

    numberFormat.CurrencyDecimalSeparator = ".";
    numberFormat.CurrencyGroupSeparator = ",";

    NumberFormat = NumberFormatInfo.ReadOnly(numberFormat);
  }
}