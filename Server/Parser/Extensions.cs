namespace NesterovskyBros.Parser;

using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using NesterovskyBros.Bidi;

public interface ITracer: IDisposable
{
  public interface IScope: IDisposable
  {
    void Add(IDisposable resource);
    void Release(IDisposable resource);
  }

  IScope? Scope(string? name, string action);
}

public interface ITraceable
{
  public string? Name { get; }
  public ITracer? Tracer { get; }
}

public class Tracer: ITracer
{
  public class Statistics
  {
    public string? Name { get; set; }
    public string? Action { get; set; }
    public long Count { get; set; }
    public long Duration { get; set; }
  }

  public Dictionary<(string? name, string? action), Statistics> 
    CollectedStatistics { get; } = new();

  public void Dispose()
  {
    foreach(var resource in resources)
    {
      resource.Dispose();
    }

    resources.Clear();
  }

  public ITracer.IScope? Scope(string? name, string? action)
  {
    if(!CollectedStatistics.TryGetValue((name, action), out var statistics))
    {
      statistics = new()
      {
        Name = name,
        Action = action
      };

      CollectedStatistics[(name, action)] = statistics;
    }

    return new TracerScope
    {
      tracer = this,
      timestamp  = Stopwatch.GetTimestamp(),
      statistics = statistics
    };
  }

  private HashSet<IDisposable> resources = 
    new(ReferenceEqualityComparer.Instance);

  private class TracerScope: ITracer.IScope
  {
    public Tracer? tracer;
    public HashSet<IDisposable> resources =
      new(ReferenceEqualityComparer.Instance);
    public Statistics? statistics;
    public long timestamp;

    public void Dispose()
    {
      ++statistics!.Count;
      statistics!.Duration += Stopwatch.GetTimestamp() - timestamp;

      foreach(var resource in resources)
      {
        tracer?.resources.Remove(resource);
        resource.Dispose();
      }

      resources.Clear();
    }

    public void Add(IDisposable resource)
    {
      resources.Add(resource);
      tracer?.resources.Add(resource);
    }

    public void Release(IDisposable resource)
    {
      tracer?.resources.Remove(resource);
      resources.Remove(resource);
    }
  }
}


/// <summary>
/// LINQ extension function to simplify streaming processing.
/// </summary>
public static class Extensions
{
  /// <summary>
  /// Wraps source enumerable into traced enumerable.
  /// </summary>
  /// <typeparam name="T">An element type of the source.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="name"></param>
  /// <param name="tracer">Optional a tracer interface.</para>
  /// </param>
  /// <returns>A enumerable wrapping source trace.</returns>
  public static IEnumerable<T> Trace<T>(
    this IEnumerable<T> source, 
    string name,
    ITracer? tracer) =>
    source is IList<T> list ? 
      new TraceableList<T, IList<T>> 
      { 
        Name = name, 
        Tracer = tracer,
        Source = list 
      } :
    source is ICollection<T> collection ? 
      new TraceableCollection<T, ICollection<T>> 
      { 
        Name = name, 
        Tracer = tracer,
        Source = collection 
      } :
      new TraceableEnumerable<T, IEnumerable<T>> 
      { 
        Name = name, 
        Tracer = tracer,
        Source = source 
      };

  /// <summary>
  /// <para>Returns enumeration with the same content with lookahead capability.</para>
  /// <para>
  /// This function lets to access top elements of original enumeration 
  /// without its re-enumumeration, e.g. with 
  /// <c>items.First()</c> or <c>items.ElementAt()</c>.
  /// </para>
  /// </summary>
  /// <remarks>
  /// Note that this function may leave source enumerator not disposed.
  /// </remarks>
  /// <typeparam name="T">An element type of the source.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="depth">A lookahead depth. Default value is 1.</param>
  /// <param name="enumerators">A number of enumerators to cache.</param>
  /// <returns>Enumerable with lookahead capability.</returns>
  public static IEnumerable<T> Lookahead<T>(
    this IEnumerable<T> source, 
    int depth = 1,
    int enumerators = 1)
  {
    if (enumerators <= 0)
    {
      enumerators = 1;
    }

    var version = 0;
    var cache = 
      new List<(IEnumerator<T> enumerator, int index, int version)>();
    var buffer = new List<T>();
    var hasMore = default(bool?);

    IEnumerable<T> processor()
    {
      var index = 0;

      while(true)
      {
        if (index < buffer.Count)
        {
          yield return buffer[index++];
        }
        else if ((index == buffer.Count) && (hasMore == false))
        {
          yield break;
        }
        // No more cases.

        var cacheIndex = -1;

        for(var i = 0; i < cache.Count; ++i)
        {
          var item = cache[i];

          if ((item.index <= index) &&
            ((cacheIndex == -1) || cache[cacheIndex].index < item.index))
          {
            cacheIndex = i;
          }
        }

        if (cacheIndex == -1)
        {
          if (cache.Count >= enumerators)
          {
            for(var i = 0; i < cache.Count; ++i)
            {
              var item = cache[i];

              if ((cacheIndex == -1) || 
                (cache[cacheIndex].version < item.version))
              {
                cacheIndex = i;
              }
            }

            cache.RemoveAt(cacheIndex);
          }

          cacheIndex = cache.Count;
          cache.Add((enumerator: source.GetEnumerator(), 0, version));
        }

        var cacheItem = cache[cacheIndex];
        var enumerator = cacheItem.enumerator;
        var enumeratorIndex = cacheItem.index;

        if (enumeratorIndex < index)
        {
          while(enumeratorIndex++ < index)
          {
            if(!enumerator.MoveNext())
            {
              throw new InvalidOperationException("Non-idempotent enumerable.");
            }
          }
        }

        if (!enumerator.MoveNext())
        {
          if (index == buffer.Count)
          {
            hasMore = false;
          }

          enumerator.Dispose();
          cache.RemoveAt(cacheIndex);
          ++version;

          yield break;
        }

        var current = enumerator.Current;

        if (index == buffer.Count)
        {
          if (index < depth)
          {
            buffer.Add(current);
          }
          else
          {
            hasMore = true;
          }
        }

        ++index;
        ++version;
        cache[cacheIndex] = (enumerator, index, version);

        yield return current;
      }
    }

    return processor();
  }

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

  /// <summary>
  /// Ranks elements of enumeration by key.
  /// </summary>
  /// <typeparam name="T">An element type.</typeparam>
  /// <typeparam name="K">A key type.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="keySelector">A key selector.</param>
  /// <returns>
  /// A enumeration of value tuples <c>(int rank, T item)</c>.
  /// </returns>
  public static IEnumerable<(int rank, T item)> RankAdjacent<T, K>(
    this IEnumerable<T> source,
    Func<T, K> keySelector)
  {
    var comparer = EqualityComparer<K>.Default;
    var rank = 0;
    var prev = default(K);

    foreach(var item in source)
    {
      var key = keySelector(item);

      if (rank == 0 || !comparer.Equals(prev, key))
      {
        ++rank;
      }

      yield return (rank, item);

      prev = key;
    }
  }

  public static IEnumerable<(int rank, T item)> RankAdjacent<T>(
    this IEnumerable<T> source,
    IEqualityComparer<T> comparer)
  {
    var rank = 0;
    var prev = default(T);

    foreach(var item in source)
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
    this IEnumerable<T> source,
    Func<T, bool>? startsAt = null,
    Func<T, bool>? endsAt = null)
  {
    var rank = 0;

    foreach(var item in source)
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

  public static IEnumerable<IEnumerable<T>> GroupAdjacent<T, K>(
    this IEnumerable<T> source,
    Func<T, K> keySelector) =>
    source.
      RankAdjacent(keySelector).
      GroupAdjacent().
      RemoveRank();

  public static IEnumerable<IEnumerable<T>> GroupAdjacent<T>(
    this IEnumerable<T> source,
    IEqualityComparer<T> comparer) =>
    source.
      RankAdjacent(comparer).
      GroupAdjacent().
      RemoveRank();

  public static IEnumerable<IEnumerable<T>> GroupAdjacent<T>(
    this IEnumerable<T> source,
    Func<T, bool>? startsAt = null,
    Func<T, bool>? endsAt = null) =>
    source.
      RankAdjacent(startsAt, endsAt).
      GroupAdjacent().
      RemoveRank();

  public static IEnumerable<IEnumerable<(int rank, T item)>> GroupAdjacent<T>(
    this IEnumerable<(int rank, T item)> source)
  {
    using var enumerator = source.GetEnumerator();

    if(!enumerator.MoveNext())
    {
      yield break;
    }

    var index = 0;
    var hasMore = true;

    IEnumerable<(int rank, T item)> group(
      (int rank, T item) current,
      int groupIndex)
    {
      yield return current;

      if(index == groupIndex)
      {
        ++index;
        hasMore = enumerator.MoveNext();

        while(hasMore && enumerator.Current.rank == current.rank)
        {
          yield return enumerator.Current;

          ++index;
          hasMore = enumerator.MoveNext();
        }
      }
      else
      {
        foreach(var item in source.Skip(groupIndex + 1))
        {
          if(item.rank != current.rank)
          {
            break;
          }

          yield return item;
        }
      }
    };

    while(hasMore)
    {
      var current = enumerator.Current;

      yield return group(current, index);

      while(hasMore && enumerator.Current.rank == current.rank)
      {
        ++index;
        hasMore = enumerator.MoveNext();
      }
    }
  }

  public static IEnumerable<IEnumerable<T>> RemoveRank<T>(
    this IEnumerable<IEnumerable<(int rank, T item)>> items) =>
    items.Select(group => group.Select(ranked => ranked.item));

  private class TraceableEnumerable<T, C>: ITraceable, IEnumerable<T>
    where C: IEnumerable<T>
  {
    public string? Name { get; init; }
    public ITracer? Tracer { get; init; }
    public C? Source { get; init; }

    public IEnumerator<T> GetEnumerator()
    {
      using var scope = 
        Tracer?.Scope(Name, first ? "GetEnumerator" : "GetEnumerator.Rescan");

      first = false;

      using var enumerator = Source!.GetEnumerator();

      scope?.Add(enumerator);

      try
      {
        while(enumerator.MoveNext())
        {
          yield return enumerator.Current;
        }
      }
      finally
      {
        scope?.Release(enumerator);
      }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private bool first = true;
  }

  private class TraceableCollection<T, C>: TraceableEnumerable<T, C>, ICollection<T>
    where C: ICollection<T>
  {
    public int Count
    {
      get
      {
        using var scope = Tracer?.Scope(Name, "Count");
        
        return Source!.Count;
      }
    }

    public bool IsReadOnly => true;

    public void Add(T item) => throw new NotImplementedException();

    public void Clear() => throw new NotImplementedException();

    public bool Contains(T item)
    {
      using var scope = Tracer?.Scope(Name, "Contains");

      return Source!.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      using var scope = Tracer?.Scope(Name, "CopyTo");

      Source!.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item) => throw new NotImplementedException();
  }

  private class TraceableList<T, C>: TraceableCollection<T, C>, IList<T>
    where C: IList<T>
  {
    public T this[int index]
    {
      get
      {
        using var scope = Tracer?.Scope(Name, "Index");

        return Source![index];
      }
      set => throw new NotImplementedException();
    }

    public int IndexOf(T item)
    {
      using var scope = Tracer?.Scope(Name, "IndexOf");
      
      return Source!.IndexOf(item);
    }

    public void Insert(int index, T item) => throw new NotImplementedException();

    public void RemoveAt(int index) => throw new NotImplementedException();
  }
}

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

public static class Functions
{
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

  public static string? NullIfEmpty(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private static readonly Regex spaces = new Regex(@"\s\s+", RegexOptions.Compiled);

  public static string Normalize(string? value)
  {
    return value == null ? "" : spaces.Replace(value, " ").Trim();
  }

  public static string? Bidi(string? value) => 
    BidiConverter.Convert(value, true, false);

  /// <summary>
  /// Converts an anonymous type to an XElement.
  /// </summary>
  /// <param name="input">The input.</param>
  /// <param name="name">The element name.</param>
  /// <returns>
  /// Returns the object as it's XML representation in an 
  /// <see cref="XElement"/>.
  /// </returns>
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

    var inputType = input.GetType();

    if (IsSimpleType(inputType))
    {
      if ((input is DateTime date) && (date.TimeOfDay == TimeSpan.Zero))
      {
        return new XElement(name, date.ToString("yyyy-MM-dd"));
      }
      else
      {
        return new XElement(name, input);
      }
    }
   
    if (IsEnumerable(inputType))
    {
      return ToXml(new { items = input }, name);
    }

    var children = new List<object>();

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

      if (IsSimpleType(type))
      {
        children.Add((value is string text) && 
          (propertyName.Equals("text", StringComparison.OrdinalIgnoreCase) ||
            propertyName.Equals("value", StringComparison.OrdinalIgnoreCase)) ?
            new XText(text) :
          (value is DateTime date) && (date.TimeOfDay == TimeSpan.Zero) ?
            new XAttribute(propertyName, date.ToString("yyyy-MM-dd")) :
            new XAttribute(propertyName, value));
      }
      else if (IsEnumerable(type))
      {
        var itemName = propertyName.EndsWith("ies") ? 
          propertyName[0..^3] + "y" :
          propertyName.EndsWith("s") ? propertyName[0..^1] :
          propertyName;

        foreach(var item in (IEnumerable)value)
        {
          children.Add(ToXml(item, itemName) ?? new XElement(itemName));
        }
      }
      else
      {
        var child = ToXml(value, propertyName);

        if (child != null)
        {
          children.Add(child);
        }
      }
    }

    return new XElement(XmlConvert.EncodeName(name), children);
  }

  private static bool IsSimpleType(Type type)
  {
    return type.IsPrimitive || type.IsEnum || WriteTypes.Contains(type);
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