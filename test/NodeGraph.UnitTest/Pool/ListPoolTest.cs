using NodeGraph.Model.Pool;

namespace NodeGraph.UnitTest.Pool;

/// <summary>
/// ListPoolのコア機能テスト
/// </summary>
public class ListPoolTest
{
    [Fact]
    public void Rent_ReturnsNonNullList()
    {
        using var rental = ListPool<int>.Shared.Rent(out var list);

        Assert.NotNull(list);
    }

    [Fact]
    public void Dispose_ReturnsToPoolAndClears()
    {
        List<int>? firstList;

        // 1回目のレンタルでアイテムを追加
        using (var rental = ListPool<int>.Shared.Rent(out var list))
        {
            list.Add(42);
            list.Add(100);
            firstList = list;
        }

        // 2回目のレンタル - リストはクリアされている
        using var rental2 = ListPool<int>.Shared.Rent(out var list2);
        Assert.Empty(list2);
    }

    [Fact]
    public void MultipleRents_ReturnDifferentInstances()
    {
        using var rental1 = ListPool<int>.Shared.Rent(out var list1);
        using var rental2 = ListPool<int>.Shared.Rent(out var list2);
        using var rental3 = ListPool<int>.Shared.Rent(out var list3);

        // 同時にレンタルした場合は異なるインスタンス
        Assert.NotSame(list1, list2);
        Assert.NotSame(list2, list3);

        // それぞれ独立して使用可能
        list1.Add(1);
        list2.Add(2);
        list3.Add(3);

        Assert.Single(list1);
        Assert.Single(list2);
        Assert.Single(list3);
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
                    using var rental = ListPool<int>.Shared.Rent(out var list);
                    list.Add(j);
                    Assert.Single(list);
                }
            }));

        await Task.WhenAll(tasks);
    }
}
