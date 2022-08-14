using System.Globalization;
using System.Text.RegularExpressions;

using NesterovskyBros.Bidi;

namespace NesterovskyBros.Utils;

/// <summary>
/// Extension functions to simplify streaming processing.
/// </summary>
public static partial class Extensions
{
  /// <summary>
  /// Default number format that uses "." as a decimal point,
  /// and "," as thousand separator.
  /// </summary>
  public static readonly NumberFormatInfo NumberFormat;

  /// <summary>
  /// <para>Gets substring out of string value.</para>
  /// <param>
  /// If <c>value</c> is <c>null</c> then empty string is returned.
  /// </param>
  /// <para>
  /// If <c>start</c> is less than zero then start is considered <c>0</c>,
  /// and length is reduced.
  /// </para>
  /// <para>
  /// If <c>start</c> is greater than the <c>value</c> length 
  /// then an empty string is returned.
  /// </para>
  /// <para>
  /// If <c>start + length</c> is greater then the <c>value</c> length then 
  /// <c>length</c> is adjusted to not be pointing out of string.
  /// </para>
  /// <para>
  /// If <c>length</c> is less than or equal to zero then empty string is returned.
  /// </para>
  /// </summary>
  /// <param name="value">A value to get substring for.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <returns>A substring value.</returns>
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

  /// <summary>
  /// Gets a string from a value.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <param name="normalize">
  /// <c>true</c> to normalize white spaced; and 
  /// </param>
  /// <param name="trim">
  /// <c>true</c> to trim leading and trailing white spaced; and 
  /// </param>
  /// <param name="bidi">
  /// <c>true</c> to convert string from visual to logical form; and 
  /// </param>
  /// <param name="nullIfEmpty">
  /// <c>true</c> to convert empty string into null.
  /// </param>
  /// <returns></returns>
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

  /// <summary>
  /// Parses a substring as an int.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <returns>A result value.</returns>
  public static int Int(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    int.Parse(Substring(value, start, length));

  /// <summary>
  /// Tries to parse a substring as an int; or return 
  /// <c>null</c> in case of failure.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <returns>A result value.</returns>
  public static int? TryInt(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    int.TryParse(
      Substring(value, start, length),
      out var result) ? result : null;

  /// <summary>
  /// Parses a substring as a long.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <returns>A result value.</returns>
  public static long Long(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    long.Parse(Substring(value, start, length));

  /// <summary>
  /// Tries to parse a substring as a long; or return 
  /// <c>null</c> in case of failure.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <returns>A result value.</returns>
  public static long? TryLong(
    string? value,
    int start = 0,
    int length = int.MaxValue) =>
    long.TryParse(
      Substring(value, start, length),
      out var result) ? result : null;

  /// <summary>
  /// Parses a substring as a decimal.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <param name="numberStyles">Optional number styles.</param>
  /// <param name="formatProvider">
  /// Optional formatter provider.
  /// If not specified then <see cref="NumberFormat"/> is used.
  /// </param>
  /// <returns>A result value.</returns>
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

  /// <summary>
  /// Tries to parse a substring as a decimal.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <param name="numberStyles">Optional number styles.</param>
  /// <param name="formatProvider">
  /// Optional formatter provider.
  /// If not specified then <see cref="NumberFormat"/> is used.
  /// </param>
  /// <returns>A result value.</returns>
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

  /// <summary>
  /// Parses a substring as a <see cref="DateTime"/>.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <param name="formats">
  /// Optional list of date formats to probe.
  /// </param>
  /// <returns>A result value.</returns>
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

  /// <summary>
  /// Tries to parse a substring as a <see cref="DateTime"/>.
  /// </summary>
  /// <param name="value">A value to get string from.</param>
  /// <param name="start">A start position.</param>
  /// <param name="length">A substring length.</param>
  /// <param name="formats">
  /// Optional list of date formats to probe.
  /// </param>
  /// <returns>A result value.</returns>
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

  /// <summary>
  /// Returns null if string is empty.
  /// </summary>
  /// <param name="value">A string to test.</param>
  /// <returns>A result value.</returns>
  public static string? NullIfEmpty(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  /// <summary>
  /// Normalizes string's whitespaces.
  /// </summary>
  /// <param name="value">A string to normalize.</param>
  /// <returns>A result value.</returns>
  public static string Normalize(string? value)
  {
    return value == null ? "" : spaces.Replace(value, " ").Trim();
  }

  /// <summary>
  /// Converts visual string into logical.
  /// </summary>
  /// <param name="value">A value to convert.</param>
  /// <returns>A result value.</returns>
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