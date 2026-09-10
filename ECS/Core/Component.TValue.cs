namespace ECS.Core;

/// <summary>
/// A component that wraps a single value and fires <see cref="ValueChanged"/> when the value changes.
/// </summary>
public abstract partial class Component<TValue>(TValue initialValue) : Component
{
    /// <summary>
    /// Raised when <see cref="Value"/> is assigned a value that differs from the current one.
    /// </summary>
    public event Action<TValue>? ValueChanged;

    /// <summary>
    /// The current value; settable by subclasses only.
    /// </summary>
    public TValue Value
    {
        get;
        protected set
        {
            if (EqualityComparer<TValue>.Default.Equals(field, value))
                return;

            field = value;
            this.ValueChanged?.Invoke(value);
        }
    } = initialValue;
}
