using System.Collections;

namespace ECS.Utils;

/// <summary>
/// A set of <typeparamref name="TElement"/> that maintains per-derived-type buckets for efficient typed lookups.
/// </summary>
public sealed class TypedSet<TElement> : ISet<TElement>, IReadonlyTypedSet<TElement>
{
    private static readonly Dictionary<Type, IEnumerable> _EMPTY_SETS = [];
    private static readonly object?[] _ARGS = [1];

    /// <inheritdoc/>
    public int Count =>
       this._root.Count;

    /// <inheritdoc/>
    public bool IsReadOnly =>
        ((ISet<TElement>)this._root).IsReadOnly;

    private readonly HashSet<TElement> _root;
    private readonly Dictionary<Type, IEnumerable> _elements = [];

    /// <summary>
    /// Initialises an empty set with a base-type bucket obtained from <see cref="ElementsPool"/>.
    /// </summary>
    public TypedSet() =>
        this._root = (HashSet<TElement>)(this._elements[typeof(TElement)] = ElementsPool.GetOrCreate<HashSet<TElement>>());

    /// <inheritdoc/>
    public bool Add(TElement item)
    {
        var result = false;

        for (var type = item?.GetType(); type != null && type != typeof(TElement).BaseType; type = type.BaseType)
        {
            if (!this._elements.TryGetValue(type, out var set))
            {
                set = (IEnumerable)ElementsPool.GetOrCreate(typeof(HashSet<>).MakeGenericType(type));
                this._elements.Add(type, set);
            }

            TypedSet<TElement>._ARGS[0] = item;
            result = result || (bool)set.GetType().GetMethod("Add")!.Invoke(set, TypedSet<TElement>._ARGS)!;
        }

        return result;
    }

    /// <inheritdoc/>
    public IReadOnlySet<TDerivedElement> GetAll<TDerivedElement>()
        where TDerivedElement : TElement
    {
        if (!TypedSet<TElement>._EMPTY_SETS.TryGetValue(typeof(TDerivedElement), out var emptySet))
        {
            emptySet = ElementsPool.GetOrCreate<HashSet<TDerivedElement>>();
            TypedSet<TElement>._EMPTY_SETS.Add(typeof(TDerivedElement), emptySet);
        }

        return (IReadOnlySet<TDerivedElement>)this._elements.GetValueOrDefault(typeof(TDerivedElement), emptySet);
    }

    /// <inheritdoc/>
    public TDerivedElement? GetOrNull<TDerivedElement>()
        where TDerivedElement : TElement =>
            this.GetAll<TDerivedElement>().SingleOrDefault();

    /// <inheritdoc/>
    public TDerivedElement Get<TDerivedElement>()
        where TDerivedElement : TElement =>
            this.GetAll<TDerivedElement>().Single();

    /// <inheritdoc/>
    public bool Remove(TElement item)
    {
        var result = false;

        for (var type = item?.GetType(); type != null && type != typeof(TElement).BaseType; type = type.BaseType)
        {
            if (this._elements.TryGetValue(type, out var set))
            {
                TypedSet<TElement>._ARGS[0] = item;
                result = result || (bool)set.GetType().GetMethod("Remove")!.Invoke(set, TypedSet<TElement>._ARGS)!;

                if ((int)set.GetType().GetProperty("Count")!.GetMethod!.Invoke(set, null)! == 0 && type != typeof(TElement))
                {
                    this._elements.Remove(type);
                    ElementsPool.Set(set);
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        foreach (var type in this._elements.Keys.ToList())
        {
            var hashSet = this._elements[type];
            hashSet.GetType().GetMethod("Clear")!.Invoke(hashSet, null);

            if (type == typeof(TElement))
                continue;

            this._elements.Remove(type);
            ElementsPool.Set(hashSet);
        }
    }

    void ICollection<TElement>.Add(TElement item) =>
        this.Add(item);

    /// <inheritdoc/>
    public bool Contains(TElement item) =>
        this._root.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(TElement[] array, int arrayIndex) =>
        this._root.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public void UnionWith(IEnumerable<TElement> other)
    {
        this._root.UnionWith(other);
        this.RebuildDerivedSets();
    }

    /// <inheritdoc/>
    public void IntersectWith(IEnumerable<TElement> other)
    {
        this._root.IntersectWith(other);
        this.RebuildDerivedSets();
    }

    /// <inheritdoc/>
    public void ExceptWith(IEnumerable<TElement> other)
    {
        this._root.ExceptWith(other);
        this.RebuildDerivedSets();
    }

    /// <inheritdoc/>
    public void SymmetricExceptWith(IEnumerable<TElement> other)
    {
        this._root.SymmetricExceptWith(other);
        this.RebuildDerivedSets();
    }

    /// <inheritdoc/>
    public bool IsSubsetOf(IEnumerable<TElement> other) =>
        this._root.IsSubsetOf(other);

    /// <inheritdoc/>
    public bool IsSupersetOf(IEnumerable<TElement> other) =>
        this._root.IsSupersetOf(other);

    /// <inheritdoc/>
    public bool IsProperSubsetOf(IEnumerable<TElement> other) =>
        this._root.IsProperSubsetOf(other);

    /// <inheritdoc/>
    public bool IsProperSupersetOf(IEnumerable<TElement> other) =>
        this._root.IsProperSupersetOf(other);

    /// <inheritdoc/>
    public bool SetEquals(IEnumerable<TElement> other) =>
        this._root.SetEquals(other);

    /// <inheritdoc/>
    public bool Overlaps(IEnumerable<TElement> other) =>
        this._root.Overlaps(other);

    /// <inheritdoc/>
    public IEnumerator<TElement> GetEnumerator() =>
        this._root.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        this._root.GetEnumerator();

    private void RebuildDerivedSets()
    {
        var elements = this._root.ToList();
        this.Clear();

        foreach (var element in elements)
            this.Add(element);
    }
}
