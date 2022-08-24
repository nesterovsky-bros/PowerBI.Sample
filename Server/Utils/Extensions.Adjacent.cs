namespace NesterovskyBros.Utils;

/// <summary>
/// Extension functions to simplify streaming processing.
/// </summary>
public static partial class Extensions
{
  /// <summary>
  /// Ranks adjacent elements of enumeration by a key.
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

      if ((rank == 0) || !comparer.Equals(prev, key))
      {
        ++rank;
      }

      yield return (rank, item);

      prev = key;
    }
  }

  /// <summary>
  /// Ranks adjacent elements of enumeration using comparer.
  /// </summary>
  /// <typeparam name="T">An element type.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="comparer">An elements comparer.</param>
  /// <returns>
  /// A enumeration of value tuples <c>(int rank, T item)</c>.
  /// </returns>
  public static IEnumerable<(int rank, T item)> RankAdjacent<T>(
    this IEnumerable<T> source,
    IEqualityComparer<T> comparer)
  {
    var rank = 0;
    var prev = default(T);

    foreach(var item in source)
    {
      if ((rank == 0) || !comparer.Equals(prev, item))
      {
        ++rank;
      }

      yield return (rank, item);

      prev = item;
    }
  }

  /// <summary>
  /// Ranks adjacent elements of enumeration using predicates 
  /// to start and end the rank.
  /// </summary>
  /// <typeparam name="T">An element type.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="startsAt">
  /// Optional predicate to start a new rank upon true result.
  /// </param>
  /// <param name="endsAt">
  /// Optional predicate to end the rank upon true result.
  /// </param>
  /// <returns>
  /// A enumeration of value tuples <c>(int rank, T item)</c>.
  /// </returns>
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

  /// <summary>
  /// Groups adjacent elements of enumeration by a key.
  /// </summary>
  /// <typeparam name="T">An element type.</typeparam>
  /// <typeparam name="K">A key type.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="keySelector">A key selector.</param>
  /// <returns>A enumeration of enumerations of elements.</returns>
  public static IEnumerable<IEnumerable<T>> GroupAdjacent<T, K>(
    this IEnumerable<T> source,
    Func<T, K> keySelector) =>
    source.
      RankAdjacent(keySelector).
      GroupAdjacent().
      RemoveRank();

  /// <summary>
  /// Groups adjacent elements of enumeration using comparer.
  /// </summary>
  /// <typeparam name="T">An element type.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="comparer">An elements comparer.</param>
  /// <returns>A enumeration of enumerations of elements.</returns>
  public static IEnumerable<IEnumerable<T>> GroupAdjacent<T>(
    this IEnumerable<T> source,
    IEqualityComparer<T> comparer) =>
    source.
      RankAdjacent(comparer).
      GroupAdjacent().
      RemoveRank();

  /// <summary>
  /// Groups adjacent elements of enumeration using predicates 
  /// to start and end the group.
  /// </summary>
  /// <typeparam name="T">An element type.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <param name="startsAt">
  /// Optional predicate to start a new group upon true result.
  /// </param>
  /// <param name="endsAt">
  /// Optional predicate to end the group upon true result.
  /// </param>
  /// <returns>A enumeration of enumerations of elements.</returns>
  public static IEnumerable<IEnumerable<T>> GroupAdjacent<T>(
    this IEnumerable<T> source,
    Func<T, bool>? startsAt = null,
    Func<T, bool>? endsAt = null) =>
    source.
      RankAdjacent(startsAt, endsAt).
      GroupAdjacent().
      RemoveRank();

  /// <summary>
  /// Converts ranked enumeration of adjcent elements into group of 
  /// enumerations by rank.
  /// </summary>
  /// <typeparam name="T">An element type.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <returns>A enumeration of enumerations of elements.</returns>
  public static IEnumerable<IEnumerable<(int rank, T item)>> GroupAdjacent<T>(
    this IEnumerable<(int rank, T item)> source)
  {
    using var enumerator = source.GetEnumerator();

    if (!enumerator.MoveNext())
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

      if (index == groupIndex)
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
          if (item.rank != current.rank)
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

  /// <summary>
  /// Converts ranked enumeration of elements into enumeration of elements.
  /// </summary>
  /// <typeparam name="T">An element type.</typeparam>
  /// <param name="source">A source enumerable.</param>
  /// <returns>An enumeration of elements.</returns>
  public static IEnumerable<IEnumerable<T>> RemoveRank<T>(
    this IEnumerable<IEnumerable<(int rank, T item)>> source) =>
    source.Select(group => group.Select(ranked => ranked.item));
}
