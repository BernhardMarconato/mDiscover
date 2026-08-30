namespace mDiscover.Core.Collections;

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

/// <summary>
/// A UI-agnostic, memory-efficient sorted and filtered view over an underlying collection.
/// Emits fine-grained delta collection changes (Add, Remove, Move) without recreating visual trees or resetting lists.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class ObservableCollectionView<T> : IList<T>, IReadOnlyList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
{
    private readonly ObservableCollection<T> _items = [];
    private readonly INotifyCollectionChanged? _sourceNotifyCollection;
    private int _deferLevel;
    private bool _pendingRefresh;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableCollectionView{T}"/> class wrapping the specified source.
    /// </summary>
    /// <param name="source">The source collection to project.</param>
    public ObservableCollectionView(IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;

        if (source is INotifyCollectionChanged ncc)
        {
            _sourceNotifyCollection = ncc;
            _sourceNotifyCollection.CollectionChanged += OnSourceCollectionChanged;
        }

        ((INotifyPropertyChanged)_items).PropertyChanged += OnItemsPropertyChanged;
        _items.CollectionChanged += OnItemsCollectionChanged;

        Refresh();
    }

    /// <summary>
    /// Gets or sets the predicate used to determine whether an item should be included in the view.
    /// </summary>
    public Predicate<T>? Filter
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnFilterOrSortChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the comparer used to order items in the view.
    /// </summary>
    public IComparer<T>? Comparer
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnFilterOrSortChanged();
            }
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the view.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => true;

    /// <summary>
    /// Gets the underlying source sequence.
    /// </summary>
    public IEnumerable<T> Source { get; }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    public T this[int index]
    {
        get => _items[index];
        set => throw new NotSupportedException("ObservableCollectionView is a read-only projection.");
    }

    object? IList.this[int index]
    {
        get => _items[index];
        set => throw new NotSupportedException("ObservableCollectionView is a read-only projection.");
    }

    /// <summary>
    /// Re-evaluates filtering and sorting against the underlying source using non-destructive delta synchronizations.
    /// </summary>
    public void Refresh()
    {
        if (_disposed)
            return;

        if (_deferLevel > 0)
        {
            _pendingRefresh = true;
            return;
        }

        var filtered = Filter == null
            ? Source
            : Source.Where(item => Filter(item));

        var desired = Comparer == null
            ? filtered.ToList()
            : [.. filtered.Order(Comparer)];

        _items.SyncTo(desired);
        _pendingRefresh = false;
    }

    /// <summary>
    /// Enters a deferred refresh scope. Multiple updates to Filter, Comparer, or source items
    /// will coalesce until the returned token is disposed.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> token.</returns>
    public IDisposable DeferRefresh()
    {
        _deferLevel++;
        return new DeferToken(this);
    }

    private void EndDefer()
    {
        if (_deferLevel > 0)
        {
            _deferLevel--;
            if (_deferLevel == 0 && _pendingRefresh)
            {
                Refresh();
            }
        }
    }

    private void OnFilterOrSortChanged()
    {
        if (_deferLevel > 0)
        {
            _pendingRefresh = true;
        }
        else
        {
            Refresh();
        }
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_disposed)
            return;

        if (_deferLevel > 0)
        {
            _pendingRefresh = true;
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                HandleSourceAdd(e.NewItems);
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                HandleSourceRemove(e.OldItems);
                break;

            case NotifyCollectionChangedAction.Replace when e.OldItems != null && e.NewItems != null:
                HandleSourceRemove(e.OldItems);
                HandleSourceAdd(e.NewItems);
                break;

            default:
                Refresh();
                break;
        }
    }

    private void HandleSourceAdd(IList newItems)
    {
        foreach (var raw in newItems)
        {
            if (raw is not T item)
                continue;

            if (Filter != null && !Filter(item))
                continue;

            if (Comparer == null)
            {
                _items.Add(item);
            }
            else
            {
                var targetIndex = FindInsertionIndex(item);
                _items.Insert(targetIndex, item);
            }
        }
    }

    private void HandleSourceRemove(IList oldItems)
    {
        foreach (var raw in oldItems)
        {
            if (raw is not T item)
                continue;

            var index = _items.IndexOf(item);
            if (index >= 0)
            {
                _items.RemoveAt(index);
            }
        }
    }

    private int FindInsertionIndex(T item)
    {
        if (Comparer == null)
            return _items.Count;

        var low = 0;
        var high = _items.Count - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) / 2);
            var cmp = Comparer.Compare(item, _items[mid]);

            if (cmp == 0)
                return mid;

            if (cmp < 0)
                high = mid - 1;
            else
                low = mid + 1;
        }

        return low;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    private void OnItemsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(T item) => _items.IndexOf(item);

    /// <inheritdoc/>
    public bool Contains(T item) => _items.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    // Non-generic IList support
    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();
    bool IList.Contains(object? value) => value is T item && Contains(item);
    int IList.IndexOf(object? value) => value is T item ? IndexOf(item) : -1;
    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();
    void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
    bool IList.IsFixedSize => false;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;

    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    void ICollection<T>.Clear() => throw new NotSupportedException();
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _sourceNotifyCollection?.CollectionChanged -= OnSourceCollectionChanged;

        ((INotifyPropertyChanged)_items).PropertyChanged -= OnItemsPropertyChanged;
        _items.CollectionChanged -= OnItemsCollectionChanged;

        GC.SuppressFinalize(this);
    }

    private sealed class DeferToken(ObservableCollectionView<T> owner) : IDisposable
    {
        private ObservableCollectionView<T>? _owner = owner;

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.EndDefer();
        }
    }
}
