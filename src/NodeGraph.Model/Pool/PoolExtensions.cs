using NodeGraph.Model.Pool;

namespace NodeGraph.Model;

public static class PoolExtensions
{
    public static ListPoolRental<T> ToListFromPool<T>(this IEnumerable<T> source, out List<T> result)
    {
        var ret =  ListPool<T>.Shared.Rent(out result);
        result.AddRange(source);
        return ret;
    }

    public static DictionaryPoolRental<TKey, TElement> ToDictionaryFromPool<TSource, TKey, TElement>(
        this IEnumerable<TSource> source, 
        Func<TSource, TKey> keySelector, 
        Func<TSource, TElement> elementSelector,
        out Dictionary<TKey, TElement> result)
        where TKey : notnull
    {
        var ret = DictionaryPool<TKey, TElement>.Shared.Rent(out result);
        foreach (var item in source)
        {
            result[keySelector(item)] = elementSelector(item);
        }
        return ret;
    }
    
    public static DictionaryPoolRental<TKey, TValue> ToDictionaryFromPool<TKey, TValue>(this IEnumerable<(TKey, TValue)> source, out Dictionary<TKey, TValue> result)
        where TKey : notnull
    {
        var ret = DictionaryPool<TKey, TValue>.Shared.Rent(out result);
        foreach (var (key, value) in source)
        {
            result[key] = value;
        }
        return ret;
    }
    
    public static HashSetPoolRental<T1> ToHashSetFromPool<T1>(this IEnumerable<T1> source, out HashSet<T1> result)
    {
        var ret = HashSetPool<T1>.Shared.Rent(out result);
        result.UnionWith(source);
        return ret;
    }
}