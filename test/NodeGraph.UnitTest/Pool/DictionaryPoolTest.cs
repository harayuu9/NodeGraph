using NodeGraph.Model.Pool;

namespace NodeGraph.UnitTest.Pool;

/// <summary>
/// DictionaryPoolのコア機能テスト
/// </summary>
public class DictionaryPoolTest
{
    [Fact]
    public void Rent_ReturnsNonNullDictionary()
    {
        using var rental = DictionaryPool<string, int>.Shared.Rent(out var dict);

        Assert.NotNull(dict);
    }

    [Fact]
    public void Dispose_ReturnsToPoolAndClears()
    {
        Dictionary<string, int>? firstDict;

        // 1回目のレンタルでアイテムを追加
        using (var rental = DictionaryPool<string, int>.Shared.Rent(out var dict))
        {
            dict["key1"] = 42;
            dict["key2"] = 100;
            firstDict = dict;
        }

        // 2回目のレンタル - 辞書はクリアされている
        using var rental2 = DictionaryPool<string, int>.Shared.Rent(out var dict2);
        Assert.Empty(dict2);
    }

    [Fact]
    public void MultipleRents_ReturnDifferentInstances()
    {
        using var rental1 = DictionaryPool<string, int>.Shared.Rent(out var dict1);
        using var rental2 = DictionaryPool<string, int>.Shared.Rent(out var dict2);
        using var rental3 = DictionaryPool<string, int>.Shared.Rent(out var dict3);

        // 同時にレンタルした場合は異なるインスタンス
        Assert.NotSame(dict1, dict2);
        Assert.NotSame(dict2, dict3);

        // それぞれ独立して使用可能
        dict1["a"] = 1;
        dict2["b"] = 2;
        dict3["c"] = 3;

        Assert.Single(dict1);
        Assert.Single(dict2);
        Assert.Single(dict3);
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
