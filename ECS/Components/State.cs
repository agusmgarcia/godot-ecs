using Godot;

namespace ECS.Components;

/// <summary>
/// Abstract base node for all pooled states managed by StateMachine.
/// </summary>
public abstract partial class State : Node
{
    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        base.Owner = base.GetParent().Owner;
    }

    /// <summary>
    /// Returns <c>false</c> to block non-forced transitions away from this state.
    /// </summary>
    public virtual bool _ReadyToTransition() => true;

    /// <summary>
    /// Copies the given params struct into the state without a full transition.
    /// </summary>
    public virtual void _CopyStateParams(ValueType stateParams) { }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        base.Owner = null;

        base._ExitTree();
    }
}
