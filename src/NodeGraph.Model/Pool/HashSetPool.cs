using System.Collections.Concurrent;

namespace NodeGraph.Model.Pool;

/// <summary>
/// マルチスレッド対応のHashSet&lt;T&gt;プール。
/// ConcurrentBagを使用してスレッドセーフな操作を保証します。
/// </summary>
/// <typeparam name="T">セット要素の型</typeparam>
internal class HashSetPool<T>
{
    /// <summary>
    /// 共有インスタンスを取得します。
    /// </summary>
    public static readonly HashSetPool<T> Shared = new();

    private readonly int _maxCapacity;
    private readonly ConcurrentBag<HashSet<T>> _pool = [];

    /// <summary>
    /// 新しいHashSetPoolインスタンスを作成します。
    /// </summary>
    /// <param name="maxCapacity">プールに保持するセットの最大容量（デフォルト: 1024）</param>
    public HashSetPool(int maxCapacity = 1024)
    {
        _maxCapacity = maxCapacity;
    }

    /// <summary>
    /// プールからセットを取得します。プールが空の場合は新しいセットを作成します。
    /// Dispose時に自動的にプールに返却されます。
    /// </summary>
    /// <param name="hashSet">取得したセットのインスタンス</param>
    /// <returns>Dispose可能なレンタルハンドル</returns>
    public HashSetPoolRental<T> Rent(out HashSet<T> hashSet)
    {
        hashSet = RentInternal();
        return new HashSetPoolRental<T>(this, hashSet);
    }

    /// <summary>
    /// プールからセットを取得します。プールが空の場合は新しいセットを指定された容量で作成します。
    /// Dispose時に自動的にプールに返却されます。
    /// </summary>
    /// <param name="capacity">初期容量</param>
    /// <param name="hashSet">取得したセットのインスタンス</param>
    /// <returns>Dispose可能なレンタルハンドル</returns>
    public HashSetPoolRental<T> Rent(int capacity, out HashSet<T> hashSet)
    {
        hashSet = RentInternal(capacity);
        return new HashSetPoolRental<T>(this, hashSet);
    }

    private HashSet<T> RentInternal()
    {
        if (_pool.TryTake(out var hashSet)) return hashSet;

        return [];
    }

    private HashSet<T> RentInternal(int capacity)
    {
        if (_pool.TryTake(out var hashSet))
        {
            if (hashSet.EnsureCapacity(0) < capacity) hashSet.EnsureCapacity(capacity);
            return hashSet;
        }

        return new HashSet<T>(capacity);
    }

    internal void Return(HashSet<T> hashSet)
    {
        // 容量が大きすぎる場合はプールに返却しない（メモリリークを防ぐ）
        if (hashSet.Count > _maxCapacity) return;

        hashSet.Clear();
        _pool.Add(hashSet);
    }
}

/// <summary>
/// HashSetPoolからレンタルしたセットのハンドル。
/// Dispose時に自動的にプールに返却されます。
/// </summary>
/// <typeparam name="T">セット要素の型</typeparam>
public struct HashSetPoolRental<T> : IDisposable
{
    private HashSetPool<T>? _pool;

    internal HashSetPoolRental(HashSetPool<T> pool, HashSet<T> hashSet)
    {
        _pool = pool;
        Value = hashSet;
    }

    /// <summary>
    /// レンタルしたセットを取得します。
    /// </summary>
    public HashSet<T>? Value { get; private set; }

    /// <summary>
    /// レンタルしたセットをプールに返却します。
    /// </summary>
    public void Dispose()
    {
        if (_pool != null && Value != null) _pool.Return(Value);
        _pool = null;
        Value = null;
    }
}