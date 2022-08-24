namespace NesterovskyBros.Utils;

using System.Collections;
using System.Diagnostics;

public interface ITracer
{
  IDisposable Scope(string name, string action, int index);
}

public interface ITraceable
{
  public string Name { get; }
  public ITracer? Tracer { get; }
}

public class Tracer: ITracer
{
  public class Statistics
  {
    public int ID { get; set; }
    public string? Caller { get; set; }
    public string Name { get; set; } = null!;
    public long Count { get; set; }
    public long DistinctCount { get; set; }
    public long Duration { get; set; }
    public List<string> Actions { get; } = new();
  }

  public Dictionary<string, Statistics> CollectedStatistics { get; } = new();

  public IDisposable Scope(string name, string action, int index)
  {
    if (!CollectedStatistics.TryGetValue(name, out var statistics))
    {
      CollectedStatistics[name] = statistics = new() 
      { 
        ID = ++lastID,
        Name = name, 
        Caller = currentScope?.statistics!.Name
      };
    }

    var scope = new TracerScope
    {
      callerScope = currentScope,
      tracer = this,
      timestamp  = Stopwatch.GetTimestamp(),
      statistics = statistics
    };

    ++statistics.Count;

    if (index == 0)
    {
      ++statistics.DistinctCount;
    }

    if (!statistics.Actions.Contains(action))
    {
      statistics.Actions.Add(action);
    }

    currentScope = scope;

    return scope;
  }

  private int lastID;
  private TracerScope? currentScope;

  private class TracerScope: IDisposable
  {
    public TracerScope? callerScope;
    public Tracer tracer = null!;
    public Statistics statistics = null!;
    public long timestamp;

    public void Dispose()
    {
      tracer!.currentScope = callerScope;
      statistics!.Duration += Stopwatch.GetTimestamp() - timestamp;
    }
  }
}

/// <summary>
/// Extension functions to simplify streaming processing.
/// </summary>
public static partial class Extensions
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

  private class TraceableEnumerable<T, C>: ITraceable, IEnumerable<T>
    where C: IEnumerable<T>
  {
    public string Name { get; init; } = null!;
    public ITracer? Tracer { get; init; }
    public C Source { get; init; } = default!;

    public IEnumerator<T> GetEnumerator()
    {
      using var scope = Tracer?.Scope(Name!, "GetEnumerator", index);

      ++index;

      foreach(var item in Source)
      {
        yield return item;
      }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected int index;
  }

  private class TraceableCollection<T, C>: 
    TraceableEnumerable<T, C>, ICollection<T>
    where C: ICollection<T>
  {
    public int Count
    {
      get
      {
        using var scope = Tracer?.Scope(Name!, "Count", index++);

        return Source.Count;
      }
    }

    public bool IsReadOnly => true;

    public void Add(T item) => throw new NotImplementedException();

    public void Clear() => throw new NotImplementedException();

    public bool Contains(T item)
    {
      using var scope = Tracer?.Scope(Name!, "Contains", index++);

      return Source.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      using var scope = Tracer?.Scope(Name!, "CopyTo", index++);

      Source.CopyTo(array, arrayIndex);
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
        using var scope = Tracer?.Scope(Name!, "Index", index++);

        return Source[index];
      }
      set => throw new NotImplementedException();
    }

    public int IndexOf(T item)
    {
      using var scope = Tracer?.Scope(Name!, "IndexOf", index++);

      return Source.IndexOf(item);
    }

    public void Insert(int index, T item) => 
      throw new NotImplementedException();

    public void RemoveAt(int index) => throw new NotImplementedException();
  }
}
