using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// Base class for all states managed by <see cref="ECS.Entities.StatesMachine"/>.
/// </summary>
[GlobalClass]
public partial class State : Component
{
    /// <summary>
    /// Returns <c>false</c> to block non-forced transitions away from this state.
    /// </summary>
    internal protected virtual bool ReadyToTransition() => true;

    internal virtual void CopyStateParams(ValueType stateParams) { }
}
