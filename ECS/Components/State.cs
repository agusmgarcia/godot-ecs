using ECS.Core;

namespace ECS.Components;

/// <summary>
/// Base class for all states managed by <see cref="ECS.Components.StateMachine"/>.
/// </summary>
public abstract partial class State : Component
{
    /// <summary>
    /// The current state machine parent.
    /// </summary>
    public StateMachine? Parent { get; private set; }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        this.Parent = base.GetParentOrNull<StateMachine>();
    }

    /// <summary>
    /// Returns <c>false</c> to block non-forced transitions away from this state.
    /// </summary>
    internal protected virtual bool ReadyToTransition() => true;

    internal virtual void CopyStateParams(ValueType stateParams) { }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.Parent = null;

        base.OnDispose();
    }
}
