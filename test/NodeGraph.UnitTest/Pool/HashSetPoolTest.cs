using NodeGraph.Model.Pool;

namespace NodeGraph.UnitTest.Pool;

/// <summary>
/// HashSetPoolのコア機能テスト
/// </summary>
public class HashSetPoolTest
{
    [Fact]
    public void Rent_ReturnsNonNullHashSet()
    {
        using var rental = HashSetPool<int>.Shared.Rent(out var set);

        Assert.NotNull(set);
    }

    [Fact]
    public void Dispose_ReturnsToPoolAndClears()
    {
        HashSet<int>? firstSet;

        // 1回目のレンタルでアイテムを追加
        using (var rental = HashSetPool<int>.Shared.Rent(out var set))
        {
            set.Add(42);
            set.Add(100);
            firstSet = set;
        }

        // 2回目のレンタル - セットはクリアされている
        using var rental2 = HashSetPool<int>.Shared.Rent(out var set2);
        Assert.Empty(set2);
    }

    [Fact]
    public void MultipleRents_ReturnDifferentInstances()
    {
        using var rental1 = HashSetPool<int>.Shared.Rent(out var set1);
        using var rental2 = HashSetPool<int>.Shared.Rent(out var set2);
        using var rental3 = HashSetPool<int>.Shared.Rent(out var set3);

        // 同時にレンタルした場合は異なるインスタンス
        Assert.NotSame(set1, set2);
        Assert.NotSame(set2, set3);

        // それぞれ独立して使用可能
        set1.Add(1);
        set2.Add(2);
        set3.Add(3);

        Assert.Single(set1);
        Assert.Single(set2);
        Assert.Single(set3);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentRentAndReturn()
    {
        var tasks = new List<Task>();

        for (var i = 0; i < 10; i++)
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    using var rental = HashSetPool<int>.Shared.Rent(out var set);
                    set.Add(j);
                    Assert.Single(set);
                }
            }));

        await Task.WhenAll(tasks);
    }
}
