using ECS.Core;
using ECS.Entities;
using Godot;

namespace ECS.Components;

/// <summary>
/// Base class for all states managed by <see cref="ECS.Entities.StatesMachine"/>.
/// </summary>
[GlobalClass]
public partial class State : Component
{
    /// <summary>
    /// The current state machine parent.
    /// </summary>
    public StatesMachine? Parent { get; private set; }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        this.Parent = (this as Node).GetParentOrNull<StatesMachine>();
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
