namespace NesterovskyBros.Utils;

/// <summary>
/// Extension functions to simplify streaming processing.
/// </summary>
public static partial class Extensions
{
  /// <summary>
  /// Projects a window of source elements in a source sequence into target sequence.
  /// </summary>
  /// <typeparam name="T">A type of elements of source sequence.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="window">A size of window.</param>
  /// <param name="lookbehind">
  /// Indicate whether to produce target if the number of source elements 
  /// preceeding the current is less than the window size.
  /// </param>
  /// <param name="lookahead">
  /// Indicate whether to produce target if the number of source elements 
  /// following current is less than the window size.
  /// </param>
  /// <returns>Returns a enumerable of <see cref="BufferSpan{T}"/> encapsulating the window.</returns>
  public static IEnumerable<BufferSpan<T>> Window<T>(
    this IEnumerable<T> source,
    int window,
    bool lookbehind,
    bool lookahead)
  {
    var buffer = new T[window];
    var index = 0;
    var count = 0;

    foreach(var value in source)
    {
      if (count < window)
      {
        buffer[count++] = value;

        if (lookbehind || count == window)
        {
          yield return new BufferSpan<T>(buffer, 0, count);
        }
      }
      else
      {
        buffer[index] = value;
        index = index + 1 == window ? 0 : index + 1;

        yield return new BufferSpan<T>(buffer, index, count);
      }
    }

    if (lookahead)
    {
      while(--count > 0)
      {
        index = index + 1 == window ? 0 : index + 1;

        yield return new BufferSpan<T>(buffer, index, count);
      }
    }
  }
}

/// <summary>
/// A value encpasulating the window of items.
/// </summary>
/// <typeparam name="T">An element type of the window.</typeparam>
public struct BufferSpan<T>
{
  /// <summary>
  /// Creates a window from array.
  /// </summary>
  /// <param name="items">An array of elements.</param>
  /// <param name="start">A window start.</param>
  /// <param name="count">A window length.</param>
  public BufferSpan(T[] items, int start, int count)
  {
    this.items = items;
    this.start = start;
    this.count = count;
  }

  /// <summary>
  /// A length of the window.
  /// </summary>
  public int Length => count;

  /// <summary>
  /// Gets an item of the window by index.
  /// </summary>
  /// <param name="index">
  /// <para>An item index.</para>
  /// <para>
  /// Index should satisfy <c>0 &lt; index && index &lt; Length</c>, otherwise
  /// behavior is undefined.
  /// </para>
  /// </param>
  /// <returns>An item for the index.</returns>
  public T this[int index]
  {
    get
    {
      var i = start + index;

      return items[i > items.Length ? i - items.Length : i];
    }
  }

  /// <summary>
  /// Gets window content into an array.
  /// </summary>
  /// <returns></returns>
  public T[] ToArray()
  {
    var result = new T[count];
    var index = start;

    for(var i = 0; i < count; ++i)
    {
      result[i] = items[index];

      if (++index == items.Length)
      {
        index = 0;
      }
    }

    return result;
  }

  private readonly T[] items;
  private readonly int start;
  private readonly int count;
}
