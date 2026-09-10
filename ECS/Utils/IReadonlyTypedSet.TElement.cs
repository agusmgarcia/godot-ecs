namespace ECS.Utils;

/// <summary>
/// Read-only view of a <see cref="TypedSet{TElement}"/> that supports typed queries by derived type.
/// </summary>
public interface IReadonlyTypedSet<TElement> : IReadOnlySet<TElement>
{
    /// <summary>
    /// Returns all elements whose concrete type is <typeparamref name="TDerivedElement"/> or a subtype of it.
    /// </summary>
    public IReadOnlySet<TDerivedElement> GetAll<TDerivedElement>()
        where TDerivedElement : TElement;

    /// <summary>
    /// Returns the single element of type <typeparamref name="TDerivedElement"/>, or <c>null</c> if none is present. Throws if more than one match exists.
    /// </summary>
    public TDerivedElement? GetOrNull<TDerivedElement>()
        where TDerivedElement : TElement;

    /// <summary>
    /// Returns the single element of type <typeparamref name="TDerivedElement"/>. Throws if zero or more than one match exists.
    /// </summary>
    public TDerivedElement Get<TDerivedElement>()
        where TDerivedElement : TElement;
}
