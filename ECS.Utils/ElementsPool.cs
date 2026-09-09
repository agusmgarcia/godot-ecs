namespace ECS.Utils;

/// <summary>
/// A static type-keyed object pool that reuses instances to avoid repeated heap allocations.
/// </summary>
public static class ElementsPool
{
    private static readonly Dictionary<Type, Stack<object>> _POOLS = [];

    /// <summary>
    /// Retrieves a pooled instance of <typeparamref name="TElement"/>, or creates one if the pool is empty.
    /// </summary>
    public static TElement GetOrCreate<TElement>()
        where TElement : new() =>
            (TElement)ElementsPool.GetOrCreate(typeof(TElement));

    /// <summary>
    /// Retrieves a pooled instance of the given type, or creates one if the pool is empty.
    /// </summary>
    public static object GetOrCreate(Type type)
    {
        if (ElementsPool._POOLS.TryGetValue(type, out var pool) && pool.Count > 0)
            return pool.Pop();

        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Type {type.Name} cannot be null");
    }

    /// <summary>
    /// Returns an instance back to the pool so it can be reused later.
    /// </summary>
    public static void Set(object element)
    {
        var type = element.GetType();

        if (!ElementsPool._POOLS.TryGetValue(type, out var pool))
        {
            pool = [];
            ElementsPool._POOLS[type] = pool;
        }

        pool.Push(element);
    }
}
