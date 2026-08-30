using System.Collections.ObjectModel;
using mDiscover.Core.Collections;

namespace mDiscover.Tests.Core;

public class ObservableCollectionViewTests
{
    [Fact]
    public void Filter_FiltersSourceItemsDynamically()
    {
        var source = new ObservableCollection<string>(["Apple", "Banana", "Apricot", "Cherry"]);
        using var view = new ObservableCollectionView<string>(source)
        {
            Filter = item => item.StartsWith("A", StringComparison.OrdinalIgnoreCase)
        };

        Assert.Equal(2, view.Count);
        Assert.Contains("Apple", view);
        Assert.Contains("Apricot", view);
        Assert.DoesNotContain("Banana", view);

        // Add to source dynamically
        source.Add("Avocado");
        Assert.Equal(3, view.Count);
        Assert.Contains("Avocado", view);

        // Add non-matching to source
        source.Add("Blueberry");
        Assert.Equal(3, view.Count);
    }

    [Fact]
    public void Comparer_SortsItemsInCorrectOrder()
    {
        var source = new ObservableCollection<int>([5, 1, 9, 3]);
        using var view = new ObservableCollectionView<int>(source)
        {
            Comparer = Comparer<int>.Default
        };

        Assert.Equal([1, 3, 5, 9], view.ToArray());

        source.Add(2);
        Assert.Equal([1, 2, 3, 5, 9], view.ToArray());
    }

    [Fact]
    public void DeferRefresh_CoalescesMultipleOperations()
    {
        var source = new ObservableCollection<int>([10, 20, 30, 40]);
        using var view = new ObservableCollectionView<int>(source);

        using (view.DeferRefresh())
        {
            view.Filter = x => x > 15;
            view.Comparer = Comparer<int>.Create((a, b) => b.CompareTo(a)); // Descending
            source.Add(50);
        }

        Assert.Equal([50, 40, 30, 20], view.ToArray());
    }
}

