using System.Collections.Concurrent;

namespace NodeGraph.Model.Pool;

/// <summary>
/// マルチスレッド対応のList&lt;T&gt;プール。
/// ConcurrentBagを使用してスレッドセーフな操作を保証します。
/// </summary>
/// <typeparam name="T">リスト要素の型</typeparam>
internal class ListPool<T>
{
    /// <summary>
    /// 共有インスタンスを取得します。
    /// </summary>
    public static readonly ListPool<T> Shared = new();

    private readonly int _maxCapacity;
    private readonly ConcurrentBag<List<T>> _pool = [];

    /// <summary>
    /// 新しいListPoolインスタンスを作成します。
    /// </summary>
    /// <param name="maxCapacity">プールに保持するリストの最大容量（デフォルト: 1024）</param>
    public ListPool(int maxCapacity = 1024)
    {
        _maxCapacity = maxCapacity;
    }

    /// <summary>
    /// プールからリストを取得します。プールが空の場合は新しいリストを作成します。
    /// Dispose時に自動的にプールに返却されます。
    /// </summary>
    /// <param name="list">取得したリストのインスタンス</param>
    /// <returns>Dispose可能なレンタルハンドル</returns>
    public ListPoolRental<T> Rent(out List<T> list)
    {
        list = RentInternal();
        return new ListPoolRental<T>(this, list);
    }

    /// <summary>
    /// プールからリストを取得します。プールが空の場合は新しいリストを指定された容量で作成します。
    /// Dispose時に自動的にプールに返却されます。
    /// </summary>
    /// <param name="capacity">初期容量</param>
    /// <param name="list">取得したリストのインスタンス</param>
    /// <returns>Dispose可能なレンタルハンドル</returns>
    public ListPoolRental<T> Rent(int capacity, out List<T> list)
    {
        list = RentInternal(capacity);
        return new ListPoolRental<T>(this, list);
    }

    private List<T> RentInternal()
    {
        return _pool.TryTake(out var list) ? list : [];
    }

    private List<T> RentInternal(int capacity)
    {
        if (_pool.TryTake(out var list))
        {
            if (list.Capacity < capacity) list.Capacity = capacity;
            return list;
        }

        return new List<T>(capacity);
    }

    internal void Return(List<T> list)
    {
        // 容量が大きすぎる場合はプールに返却しない（メモリリークを防ぐ）
        if (list.Capacity > _maxCapacity) return;

        list.Clear();
        _pool.Add(list);
    }
}

/// <summary>
/// ListPoolからレンタルしたリストのハンドル。
/// Dispose時に自動的にプールに返却されます。
/// </summary>
/// <typeparam name="T">リスト要素の型</typeparam>
public struct ListPoolRental<T> : IDisposable
{
    private ListPool<T>? _pool;

    internal ListPoolRental(ListPool<T> pool, List<T> list)
    {
        _pool = pool;
        Value = list;
    }

    /// <summary>
    /// レンタルしたリストを取得します。
    /// </summary>
    public List<T>? Value { get; private set; }

    /// <summary>
    /// レンタルしたリストをプールに返却します。
    /// </summary>
    public void Dispose()
    {
        if (_pool != null && Value != null) _pool.Return(Value);
        _pool = null;
        Value = null;
    }
}