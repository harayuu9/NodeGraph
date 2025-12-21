using NodeGraph.Model.Pool;

namespace NodeGraph.UnitTest.Pool;

public class HashSetPoolTest
{
    [Fact]
    public void Rent_ReturnsNonNullHashSet()
    {
        using var rental = HashSetPool<int>.Shared.Rent(out var set);

        Assert.NotNull(set);
    }

    [Fact]
    public void Rent_WithCapacity_ReturnsHashSetWithSufficientCapacity()
    {
        using var rental = HashSetPool<int>.Shared.Rent(100, out var set);

        Assert.NotNull(set);
        Assert.True(set.EnsureCapacity(0) >= 100);
    }

    [Fact]
    public void Rent_CanAddItemsToHashSet()
    {
        using var rental = HashSetPool<int>.Shared.Rent(out var set);

        set.Add(1);
        set.Add(2);
        set.Add(3);

        Assert.Equal(3, set.Count);
        Assert.Contains(1, set);
        Assert.Contains(2, set);
        Assert.Contains(3, set);
    }

    [Fact]
    public void Rent_EnsuresUniqueness()
    {
        using var rental = HashSetPool<int>.Shared.Rent(out var set);

        set.Add(1);
        set.Add(1);
        set.Add(2);

        Assert.Equal(2, set.Count);
        Assert.Contains(1, set);
        Assert.Contains(2, set);
    }

    [Fact]
    public void Dispose_ReturnsHashSetToPool()
    {
        HashSet<int>? firstSet;

        // 1回目のレンタル
        using (var rental = HashSetPool<int>.Shared.Rent(out var set))
        {
            set.Add(42);
            firstSet = set;
        }

        // 2回目のレンタル - 同じインスタンスが返される可能性がある
        using var rental2 = HashSetPool<int>.Shared.Rent(out var set2);

        // セットはクリアされているはず
        Assert.Empty(set2);
    }

    [Fact]
    public void MultipleRents_WorksConcurrently()
    {
        using var rental1 = HashSetPool<int>.Shared.Rent(out var set1);
        using var rental2 = HashSetPool<int>.Shared.Rent(out var set2);
        using var rental3 = HashSetPool<int>.Shared.Rent(out var set3);

        set1.Add(1);
        set2.Add(2);
        set3.Add(3);

        Assert.Single(set1);
        Assert.Single(set2);
        Assert.Single(set3);
        Assert.Contains(1, set1);
        Assert.Contains(2, set2);
        Assert.Contains(3, set3);
    }

    [Fact]
    public void Rent_AfterDispose_ReturnsCleanHashSet()
    {
        using (var rental = HashSetPool<int>.Shared.Rent(out var set))
        {
            set.Add(1);
            set.Add(2);
            set.Add(3);
        }

        using var rental2 = HashSetPool<int>.Shared.Rent(out var set2);

        // プールから返されたセットはクリアされているべき
        Assert.Empty(set2);
    }

    [Fact]
    public void Rent_WithLargeCapacity_WorksCorrectly()
    {
        using var rental = HashSetPool<int>.Shared.Rent(10000, out var set);

        for (var i = 0; i < 5000; i++) set.Add(i);

        Assert.Equal(5000, set.Count);
    }

    [Fact]
    public void Rent_SupportsRemove()
    {
        using var rental = HashSetPool<int>.Shared.Rent(out var set);

        set.Add(1);
        set.Add(2);
        Assert.Equal(2, set.Count);

        set.Remove(1);
        Assert.Single(set);
        Assert.DoesNotContain(1, set);
        Assert.Contains(2, set);
    }

    [Fact]
    public void Rent_SupportsSetOperations()
    {
        using var rental1 = HashSetPool<int>.Shared.Rent(out var set1);
        using var rental2 = HashSetPool<int>.Shared.Rent(out var set2);

        set1.Add(1);
        set1.Add(2);
        set1.Add(3);

        set2.Add(2);
        set2.Add(3);
        set2.Add(4);

        set1.IntersectWith(set2);

        Assert.Equal(2, set1.Count);
        Assert.Contains(2, set1);
        Assert.Contains(3, set1);
        Assert.DoesNotContain(1, set1);
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

    [Fact]
    public void Rent_SupportsStringHashSet()
    {
        using var rental = HashSetPool<string>.Shared.Rent(out var set);

        set.Add("apple");
        set.Add("banana");
        set.Add("cherry");

        Assert.Equal(3, set.Count);
        Assert.Contains("apple", set);
        Assert.Contains("banana", set);
        Assert.Contains("cherry", set);
    }
}