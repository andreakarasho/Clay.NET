using Clay;

namespace Clay.Test;

public class ClayListTests
{
    [Fact]
    public void Add_IncreasesLength()
    {
        var list = new ClayList<int>();
        Assert.Equal(0, list.Length);

        list.Add(42);
        Assert.Equal(1, list.Length);
    }

    [Fact]
    public void Add_ReturnsIndex()
    {
        var list = new ClayList<int>();
        int idx = list.Add(10);
        Assert.Equal(0, idx);

        idx = list.Add(20);
        Assert.Equal(1, idx);
    }

    [Fact]
    public void Indexer_ReturnsCorrectValue()
    {
        var list = new ClayList<int>();
        list.Add(100);
        list.Add(200);
        list.Add(300);

        Assert.Equal(100, list[0]);
        Assert.Equal(200, list[1]);
        Assert.Equal(300, list[2]);
    }

    [Fact]
    public void Indexer_RefAccess_AllowsMutation()
    {
        var list = new ClayList<int>();
        list.Add(10);

        ref int val = ref list[0];
        val = 99;

        Assert.Equal(99, list[0]);
    }

    [Fact]
    public void Clear_ResetsLength()
    {
        var list = new ClayList<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.Clear();
        Assert.Equal(0, list.Length);
    }

    [Fact]
    public void Set_SetsValueAtIndex()
    {
        var list = new ClayList<int>();
        list.Set(5, 42);
        Assert.Equal(42, list[5]);
        Assert.True(list.Length >= 6);
    }

    [Fact]
    public void RemoveSwapback_RemovesAndSwaps()
    {
        var list = new ClayList<int>();
        list.Add(10);
        list.Add(20);
        list.Add(30);

        var removed = list.RemoveSwapback(0);
        Assert.Equal(10, removed);
        Assert.Equal(2, list.Length);
        // Last element (30) should have been swapped to index 0
        Assert.Equal(30, list[0]);
    }

    [Fact]
    public void GetValue_ReturnsCorrectValue()
    {
        var list = new ClayList<int>();
        list.Add(42);
        Assert.Equal(42, list.GetValue(0));
    }

    [Fact]
    public void AsSpan_ReturnsCorrectSlice()
    {
        var list = new ClayList<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var span = list.AsSpan();
        Assert.Equal(3, span.Length);
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
    }

    [Fact]
    public void IsEmpty_WhenEmpty_ReturnsTrue()
    {
        var list = new ClayList<int>();
        Assert.True(list.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WhenNotEmpty_ReturnsFalse()
    {
        var list = new ClayList<int>();
        list.Add(1);
        Assert.False(list.IsEmpty);
    }

    [Fact]
    public void LargeNumberOfItems_HandlesGrowth()
    {
        var list = new ClayList<int>(4); // Small initial capacity
        for (int i = 0; i < 1000; i++)
        {
            list.Add(i);
        }

        Assert.Equal(1000, list.Length);
        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(i, list[i]);
        }
    }

    [Fact]
    public void Capacity_GreaterOrEqualToLength()
    {
        var list = new ClayList<int>(8);
        for (int i = 0; i < 20; i++)
            list.Add(i);

        Assert.True(list.Capacity >= list.Length);
    }
}
