using System.Collections.Concurrent;

namespace NodeGraph.Model.Pool;

/// <summary>
/// マルチスレッド対応のDictionary&lt;TKey, TValue&gt;プール。
/// ConcurrentBagを使用してスレッドセーフな操作を保証します。
/// </summary>
/// <typeparam name="TKey">辞書のキーの型</typeparam>
/// <typeparam name="TValue">辞書の値の型</typeparam>
internal class DictionaryPool<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// 共有インスタンスを取得します。
    /// </summary>
    public static readonly DictionaryPool<TKey, TValue> Shared = new();

    private readonly int _maxCapacity;
    private readonly ConcurrentBag<Dictionary<TKey, TValue>> _pool = [];

    /// <summary>
    /// 新しいDictionaryPoolインスタンスを作成します。
    /// </summary>
    /// <param name="maxCapacity">プールに保持する辞書の最大容量（デフォルト: 1024）</param>
    public DictionaryPool(int maxCapacity = 1024)
    {
        _maxCapacity = maxCapacity;
    }

    /// <summary>
    /// プールから辞書を取得します。プールが空の場合は新しい辞書を作成します。
    /// Dispose時に自動的にプールに返却されます。
    /// </summary>
    /// <param name="dictionary">取得した辞書のインスタンス</param>
    /// <returns>Dispose可能なレンタルハンドル</returns>
    public DictionaryPoolRental<TKey, TValue> Rent(out Dictionary<TKey, TValue> dictionary)
    {
        dictionary = RentInternal();
        return new DictionaryPoolRental<TKey, TValue>(this, dictionary);
    }

    /// <summary>
    /// プールから辞書を取得します。プールが空の場合は新しい辞書を指定された容量で作成します。
    /// Dispose時に自動的にプールに返却されます。
    /// </summary>
    /// <param name="capacity">初期容量</param>
    /// <param name="dictionary">取得した辞書のインスタンス</param>
    /// <returns>Dispose可能なレンタルハンドル</returns>
    public DictionaryPoolRental<TKey, TValue> Rent(int capacity, out Dictionary<TKey, TValue> dictionary)
    {
        dictionary = RentInternal(capacity);
        return new DictionaryPoolRental<TKey, TValue>(this, dictionary);
    }

    private Dictionary<TKey, TValue> RentInternal()
    {
        if (_pool.TryTake(out var dictionary)) return dictionary;

        return new Dictionary<TKey, TValue>();
    }

    private Dictionary<TKey, TValue> RentInternal(int capacity)
    {
        if (_pool.TryTake(out var dictionary))
        {
            if (dictionary.EnsureCapacity(0) < capacity) dictionary.EnsureCapacity(capacity);
            return dictionary;
        }

        return new Dictionary<TKey, TValue>(capacity);
    }

    internal void Return(Dictionary<TKey, TValue> dictionary)
    {
        // 容量が大きすぎる場合はプールに返却しない（メモリリークを防ぐ）
        if (dictionary.Count > _maxCapacity) return;

        dictionary.Clear();
        _pool.Add(dictionary);
    }
}

/// <summary>
/// DictionaryPoolからレンタルした辞書のハンドル。
/// Dispose時に自動的にプールに返却されます。
/// </summary>
/// <typeparam name="TKey">辞書のキーの型</typeparam>
/// <typeparam name="TValue">辞書の値の型</typeparam>
public struct DictionaryPoolRental<TKey, TValue> : IDisposable
    where TKey : notnull
{
    private DictionaryPool<TKey, TValue>? _pool;
    private Dictionary<TKey, TValue>? _dictionary;

    internal DictionaryPoolRental(DictionaryPool<TKey, TValue>? pool, Dictionary<TKey, TValue> dictionary)
    {
        _pool = pool;
        _dictionary = dictionary;
    }

    /// <summary>
    /// レンタルした辞書をプールに返却します。
    /// </summary>
    public void Dispose()
    {
        if (_pool != null && _dictionary != null) _pool.Return(_dictionary);
        _pool = null;
        _dictionary = null;
    }
}