namespace NesterovskyBros.Utils;

/// <summary>
/// Extension functions to simplify streaming processing.
/// </summary>
public static partial class Extensions
{
  /// <summary>
  /// <para>Returns enumeration with the same content with lookahead capability.</para>
  /// <para>
  /// This function lets to access top elements of original enumeration 
  /// without its re-enumumeration, e.g. with 
  /// <c>items.First()</c> or <c>items.ElementAt()</c>.
  /// </para>
  /// <para>
  /// <b>Note:</b> wraping enumerable with <c>Lookahaead()</c> in certain cases 
  /// may leave source enumerator not disposed.
  /// </para>
  /// </summary>
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
            if (!enumerator.MoveNext())
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
}
