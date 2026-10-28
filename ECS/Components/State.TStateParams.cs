namespace ECS.Components;

/// <summary>
/// Base class for states that carry a <typeparamref name="TStateParams"/> struct passed on each transition.
/// </summary>
public abstract partial class State<TStateParams> : State
    where TStateParams : struct
{
    /// <summary>
    /// Holds the params struct passed to this state on the last transition.
    /// </summary>
    public TStateParams StateParams { get; private set; } = default;

    /// <inheritdoc/>
    public sealed override void _CopyStateParams(ValueType stateParams) =>
        this.StateParams = (TStateParams)stateParams;
}