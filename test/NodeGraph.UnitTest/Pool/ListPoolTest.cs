using NodeGraph.Model.Pool;

namespace NodeGraph.UnitTest.Pool;

public class ListPoolTest
{
    [Fact]
    public void Rent_ReturnsNonNullList()
    {
        using var rental = ListPool<int>.Shared.Rent(out var list);

        Assert.NotNull(list);
    }

    [Fact]
    public void Rent_WithCapacity_ReturnsListWithSufficientCapacity()
    {
        using var rental = ListPool<int>.Shared.Rent(100, out var list);

        Assert.NotNull(list);
        Assert.True(list.Capacity >= 100);
    }

    [Fact]
    public void Rent_CanAddItemsToList()
    {
        using var rental = ListPool<int>.Shared.Rent(out var list);

        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Dispose_ReturnsListToPool()
    {
        List<int>? firstList;

        // 1回目のレンタル
        using (var rental = ListPool<int>.Shared.Rent(out var list))
        {
            list.Add(42);
            firstList = list;
        }

        // 2回目のレンタル - 同じインスタンスが返される可能性がある
        using var rental2 = ListPool<int>.Shared.Rent(out var list2);

        // リストはクリアされているはず
        Assert.Empty(list2);
    }

    [Fact]
    public void MultipleRents_WorksConcurrently()
    {
        using var rental1 = ListPool<int>.Shared.Rent(out var list1);
        using var rental2 = ListPool<int>.Shared.Rent(out var list2);
        using var rental3 = ListPool<int>.Shared.Rent(out var list3);

        list1.Add(1);
        list2.Add(2);
        list3.Add(3);

        Assert.Single(list1);
        Assert.Single(list2);
        Assert.Single(list3);
        Assert.Equal(1, list1[0]);
        Assert.Equal(2, list2[0]);
        Assert.Equal(3, list3[0]);
    }

    [Fact]
    public void Rent_AfterDispose_ReturnsCleanList()
    {
        using (var rental = ListPool<int>.Shared.Rent(out var list))
        {
            list.Add(1);
            list.Add(2);
            list.Add(3);
        }

        using var rental2 = ListPool<int>.Shared.Rent(out var list2);

        // プールから返されたリストはクリアされているべき
        Assert.Empty(list2);
    }

    [Fact]
    public void Rent_WithLargeCapacity_WorksCorrectly()
    {
        using var rental = ListPool<int>.Shared.Rent(10000, out var list);

        for (var i = 0; i < 5000; i++)
        {
            list.Add(i);
        }

        Assert.Equal(5000, list.Count);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentRentAndReturn()
    {
        var tasks = new List<Task>();

        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    using var rental = ListPool<int>.Shared.Rent(out var list);
                    list.Add(j);
                    Assert.Single(list);
                }
            }));
        }

        await Task.WhenAll(tasks);
    }
}
