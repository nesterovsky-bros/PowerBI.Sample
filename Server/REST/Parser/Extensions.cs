using System.Collections;
using System.Xml;
using System.Xml.Linq;

namespace REST.Parser;

public struct BufferSpan<T>
{
  public BufferSpan(T[] items, int start, int count)
  {
    this.items = items;
    this.start = start;
    this.count = count;
  }

  public int Length => count;

  public T this[int index]
  {
    get
    {
      var i = start + index;

      return items[i > items.Length ? i - items.Length : i];
    }
    set
    {
      var i = start + index;

      items[i > items.Length ? i - items.Length : i] = value;
    }
  }

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

public static class Extensions
{
  /// <summary>
  /// Projects a window of source elements in a source sequence into target sequence.
  /// </summary>
  /// <typeparam name="T">A type of elements of source sequence.</typeparam>
  /// <param name="window">A size of window.</param>
  /// <param name="lookbehind">
  /// Indicate whether to produce target if the number of source elements 
  /// preceeding the current is less than the window size.
  /// </param>
  /// <param name="lookahead">
  /// Indicate whether to produce target if the number of source elements 
  /// following current is less than the window size.
  /// </param>
  /// <returns>Returns a sequence of target elements.</returns>
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

  public static IEnumerable<T> Lookahead<T>(this IEnumerable<T> items, int capacity)
  {
    var buffer = default(List<T>);
    var hasMore = false;

    IEnumerable<T> processor()
    {
      if (buffer == null)
      {
        buffer = new();

        var index = 0;

        foreach(var item in items)
        {
          ++index;

          if (index <= capacity)
          {
            buffer.Add(item);
          }
          else
          {
            if (index == capacity + 1)
            {
              hasMore = true;

              foreach(var bufferedItem in buffer)
              {
                yield return bufferedItem;
              }
            }

            yield return item;
          }
        }

        if (!hasMore)
        {
          foreach(var bufferedItem in buffer)
          {
            yield return bufferedItem;
          }
        }
      }
      else
      {
        foreach(var bufferedItem in buffer)
        {
          yield return bufferedItem;
        }

        if (hasMore)
        {
          foreach(var item in items.Skip(capacity))
          {
            yield return item;
          }
        }
      }
    }

    return processor();
  }

  public static IEnumerable<(int rank, T item)> RankAdjacent<T, K>(
    this IEnumerable<T> items,
    Func<T, K> keySelector)
    where K : IEquatable<K>
  {
    var rank = 0;
    var prev = default(K);

    foreach(var item in items)
    {
      var key = keySelector(item);

      if (rank == 0 || !key.Equals(prev))
      {
        ++rank;
      }

      yield return (rank, item);

      prev = key;
    }
  }

  public static IEnumerable<(int rank, T item)> RankAdjacent<T>(
    this IEnumerable<T> items,
    IEqualityComparer<T> comparer)
  {
    var rank = 0;
    var prev = default(T);

    foreach(var item in items)
    {
      if (rank == 0 || !comparer.Equals(prev, item))
      {
        ++rank;
      }

      yield return (rank, item);

      prev = item;
    }
  }

  public static IEnumerable<(int rank, T item)> RankAdjacent<T>(
    this IEnumerable<T> items,
    Func<T, bool>? startsAt = null,
    Func<T, bool>? endsAt = null)
  {
    var rank = 0;

    foreach(var item in items)
    {
      if (rank == 0 ||
        startsAt?.Invoke(item) == true ||
        endsAt?.Invoke(item) == true)
      {
        ++rank;
      }

      yield return (rank, item);
    }
  }

  public static IEnumerable<(T head, IEnumerable<T> items)> GroupAdjacent<T, K>(
    this IEnumerable<T> items,
    Func<T, K> keySelector)
    where K : IEquatable<K> =>
    items.
      RankAdjacent(keySelector).
      GroupAdjacent().
      Select(item =>
        (
          head: item.head.item,
          items: item.items.Select(ranked => ranked.item)
        ));

  public static IEnumerable<(T head, IEnumerable<T> items)> GroupAdjacent<T>(
    this IEnumerable<T> items,
    IEqualityComparer<T> comparer) =>
    items.
      RankAdjacent(comparer).
      GroupAdjacent().
      Select(item =>
        (
          head: item.head.item,
          items: item.items.Select(ranked => ranked.item)
        ));

  public static IEnumerable<(T head, IEnumerable<T> items)> GroupAdjacent<T>(
    this IEnumerable<T> items,
    Func<T, bool>? startsAt = null,
    Func<T, bool>? endsAt = null) =>
    items.
      RankAdjacent(startsAt, endsAt).
      GroupAdjacent().
      Select(item =>
        (
          head: item.head.item,
          items: item.items.Select(ranked => ranked.item)
        ));

  public static IEnumerable<((int rank, T item) head, IEnumerable<(int rank, T item)> items)>
    GroupAdjacent<T>(this IEnumerable<(int rank, T item)> items)
  {
    using var enumerator = items.GetEnumerator();

    if (!enumerator.MoveNext())
    {
      yield break;
    }

    var index = 0;
    var groupConsumed = false;
    var consumed = false;

    IEnumerable<(int rank, T item)> group(int rank, int groupIndex)
    {
      while(true)
      {
        yield return enumerator.Current;

        if (index != groupIndex)
        {
          throw new InvalidOperationException("Enumerator consumed.");
        }

        if (!enumerator.MoveNext())
        {
          consumed = true;

          break;
        }

        ++index;
        ++groupIndex;

        if (enumerator.Current.rank != rank)
        {
          break;
        }
      }

      groupConsumed = true;
    };

    while(!consumed)
    {
      groupConsumed = false;

      var head = enumerator.Current;
      var rank = head.rank;

      yield return (head, items: group(rank, index));

      if (!groupConsumed)
      {
        while(true)
        {
          if (!enumerator.MoveNext())
          {
            consumed = true;

            break;
          }

          ++index;

          if (enumerator.Current.rank != rank)
          {
            break;
          }
        }
      }
    }
  }
}

public static class Functions
{
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

  /// <summary>
  /// Converts an anonymous type to an XElement.
  /// </summary>
  /// <param name="input">The input.</param>
  /// <param name="name">The element name.</param>
  /// <returns>Returns the object as it's XML representation in an XElement.</returns>
  public static XElement? ToXml(object? input, string? name = null)
  {
    if (input == null)
    {
      return null;
    }

    if (string.IsNullOrEmpty(name))
    {
      var typeName = input.GetType().Name;

      name = typeName.Contains("AnonymousType") ? "Object" : typeName;
    }

    var children = new List<XElement?>();

    foreach(var property in input.GetType().GetProperties())
    {
      var value = property.GetValue(input, null);

      if (value == null)
      {
        continue;
      }

      var propertyName = XmlConvert.EncodeName(property.Name);
      var type = Nullable.GetUnderlyingType(property.PropertyType) ??
        property.PropertyType;

      if (IsEnumerable(type))
      {
        foreach(var item in (IEnumerable)value)
        {
          children.Add(ToXml(item, null));
        }
      }
      else if (IsSimpleType(type) || type.IsEnum)
      {
        children.Add(new XElement(propertyName, value));
      }
      else
      {
        children.Add(ToXml(value, propertyName));
      }
    }

    return new XElement(XmlConvert.EncodeName(name), children.ToArray());
  }

  private static bool IsSimpleType(Type type)
  {
    return type.IsPrimitive || WriteTypes.Contains(type);
  }

  private static bool IsEnumerable(Type type)
  {
    return typeof(IEnumerable).IsAssignableFrom(type) &&
      !FlatternTypes.Contains(type);
  }

  private static readonly Type[] WriteTypes = new[]
  {
    typeof(string),
    typeof(DateTime),
    typeof(Enum),
    typeof(decimal),
    typeof(Guid),
  };

  private static readonly Type[] FlatternTypes = new[] { typeof(string) };
}