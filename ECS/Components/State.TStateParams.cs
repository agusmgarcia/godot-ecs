namespace ECS.Components;

/// <summary>
/// Base class for states that carry a <typeparamref name="TStateParams"/> struct passed on each transition.
/// </summary>
public abstract partial class State<TStateParams> : State
    where TStateParams : struct
{
    /// <summary>
    /// Parameters supplied by the last <see cref="ECS.Entities.StatesMachine.SetState{TNewState, TStateParams}"/> call.
    /// </summary>
    public TStateParams StateParams { get; internal set; } = default;

    internal sealed override void CopyStateParams(ValueType stateParams) =>
        this.StateParams = (TStateParams)stateParams;
}