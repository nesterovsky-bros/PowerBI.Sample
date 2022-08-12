namespace NesterovskyBros.Utils;

using System.Collections;
using System.Diagnostics;

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

  public Statistics[] GetStatisticsByPath()
  {
    var items = CollectedStatistics.Values.
      Select(item => new Statistics
      {
        Name = item.Name,
        Action = item.Action,
        Count = item.Count,
        Duration = item.Duration
      }).
      ToArray();

    var found = true;

    while(found)
    {
      found = false;

      foreach(var item in items)
      {
        if ((item.Name == null) || item.Name.StartsWith('/'))
        {
          continue;
        }

        var p = item.Name?.IndexOf('/') ?? -1;

        if (p != -1)
        {
          var prefix = "/" + item.Name![0..p];

          var path = items.
            FirstOrDefault(other =>
              (other != item) &&
              (other.Name?.EndsWith(prefix) == true))?.
            Name;

          if (path != null)
          {
            found = true;
            item.Name = path + item.Name[p..];
          }
        }
      }
    }

    Array.Sort(items, (f, s) => string.Compare(f.Name, s.Name));

    return items;
  }

  public ITracer.IScope? Scope(string? name, string? action)
  {
    if (!CollectedStatistics.TryGetValue((name, action), out var statistics))
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

  private class TraceableEnumerable<T, C> : ITraceable, IEnumerable<T>
    where C : IEnumerable<T>
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

  private class TraceableCollection<T, C> : TraceableEnumerable<T, C>, ICollection<T>
    where C : ICollection<T>
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

  private class TraceableList<T, C> : TraceableCollection<T, C>, IList<T>
    where C : IList<T>
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
