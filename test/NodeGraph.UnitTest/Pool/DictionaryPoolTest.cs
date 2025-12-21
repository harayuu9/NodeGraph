using NodeGraph.Model.Pool;

namespace NodeGraph.UnitTest.Pool;

public class DictionaryPoolTest
{
    [Fact]
    public void Rent_ReturnsNonNullDictionary()
    {
        using var rental = DictionaryPool<string, int>.Shared.Rent(out var dict);

        Assert.NotNull(dict);
    }

    [Fact]
    public void Rent_WithCapacity_ReturnsDictionaryWithSufficientCapacity()
    {
        using var rental = DictionaryPool<string, int>.Shared.Rent(100, out var dict);

        Assert.NotNull(dict);
        Assert.True(dict.EnsureCapacity(0) >= 100);
    }

    [Fact]
    public void Rent_CanAddItemsToDictionary()
    {
        using var rental = DictionaryPool<string, int>.Shared.Rent(out var dict);

        dict["key1"] = 1;
        dict["key2"] = 2;
        dict["key3"] = 3;

        Assert.Equal(3, dict.Count);
        Assert.Equal(1, dict["key1"]);
        Assert.Equal(2, dict["key2"]);
        Assert.Equal(3, dict["key3"]);
    }

    [Fact]
    public void Dispose_ReturnsDictionaryToPool()
    {
        Dictionary<string, int>? firstDict;

        // 1回目のレンタル
        using (var rental = DictionaryPool<string, int>.Shared.Rent(out var dict))
        {
            dict["key"] = 42;
            firstDict = dict;
        }

        // 2回目のレンタル - 同じインスタンスが返される可能性がある
        using var rental2 = DictionaryPool<string, int>.Shared.Rent(out var dict2);

        // 辞書はクリアされているはず
        Assert.Empty(dict2);
    }

    [Fact]
    public void MultipleRents_WorksConcurrently()
    {
        using var rental1 = DictionaryPool<string, int>.Shared.Rent(out var dict1);
        using var rental2 = DictionaryPool<string, int>.Shared.Rent(out var dict2);
        using var rental3 = DictionaryPool<string, int>.Shared.Rent(out var dict3);

        dict1["a"] = 1;
        dict2["b"] = 2;
        dict3["c"] = 3;

        Assert.Single(dict1);
        Assert.Single(dict2);
        Assert.Single(dict3);
        Assert.Equal(1, dict1["a"]);
        Assert.Equal(2, dict2["b"]);
        Assert.Equal(3, dict3["c"]);
    }

    [Fact]
    public void Rent_AfterDispose_ReturnsCleanDictionary()
    {
        using (var rental = DictionaryPool<string, int>.Shared.Rent(out var dict))
        {
            dict["key1"] = 1;
            dict["key2"] = 2;
            dict["key3"] = 3;
        }

        using var rental2 = DictionaryPool<string, int>.Shared.Rent(out var dict2);

        // プールから返された辞書はクリアされているべき
        Assert.Empty(dict2);
    }

    [Fact]
    public void Rent_WithLargeCapacity_WorksCorrectly()
    {
        using var rental = DictionaryPool<string, int>.Shared.Rent(10000, out var dict);

        for (var i = 0; i < 5000; i++) dict[$"key{i}"] = i;

        Assert.Equal(5000, dict.Count);
    }

    [Fact]
    public void Rent_SupportsValueUpdate()
    {
        using var rental = DictionaryPool<string, int>.Shared.Rent(out var dict);

        dict["key"] = 1;
        Assert.Equal(1, dict["key"]);

        dict["key"] = 2;
        Assert.Equal(2, dict["key"]);
    }

    [Fact]
    public void Rent_SupportsContainsKey()
    {
        using var rental = DictionaryPool<string, int>.Shared.Rent(out var dict);

        dict["key1"] = 1;

        Assert.True(dict.ContainsKey("key1"));
        Assert.False(dict.ContainsKey("key2"));
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
                    using var rental = DictionaryPool<string, int>.Shared.Rent(out var dict);
                    dict[$"key{j}"] = j;
                    Assert.Single(dict);
                }
            }));

        await Task.WhenAll(tasks);
    }
}