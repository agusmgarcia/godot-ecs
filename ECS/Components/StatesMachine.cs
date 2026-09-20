using ECS.Core;
using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that acts as a pooled finite state machine; manages a single active <see cref="ECS.Components.State"/> child component and transitions between states each physics frame.
/// </summary>
public abstract partial class StatesMachine : Component<State?>
{
    /// <summary>
    /// Initialises the component with an empty state.
    /// </summary>
    protected StatesMachine()
        : base(null) { }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        base.ChildExitingTree += this.OnStateRemoved;
        base.ChildEnteredTree += this.OnStateAdded;
        this.OnStateAdded(base.GetChildOrNull<Node>(0));
    }

    private void OnStateAdded(Node? node)
    {
        if (node is not State state)
            return;

        base.Value = state;
    }

    /// <summary>
    /// Transitions to <typeparamref name="TNewState"/>, pooling the current state and initialising the new one with <paramref name="stateParams"/>; no-ops if the current state blocks transitions via <see cref="ECS.Components.State.ReadyToTransition"/> and <paramref name="force"/> is <c>false</c>.
    /// </summary>
    protected void SetState<TNewState, TStateParams>(in TStateParams stateParams, bool force = false)
        where TNewState : State<TStateParams>, new()
        where TStateParams : struct
    {
        var currentState = base.GetChildOrNull<State>(0);
        if (!force && currentState != null && !currentState.ReadyToTransition())
            return;

        if (currentState != null && currentState.GetType() == typeof(TNewState))
        {
            currentState.CopyStateParams(stateParams);
            return;
        }

        if (currentState != null)
        {
            base.RemoveChild(currentState);
            ElementsPool.Set(currentState);
        }

        var newState = ElementsPool.GetOrCreate<TNewState>();
        newState.StateParams = stateParams;
        base.AddChild(newState);
    }

    private void OnStateRemoved(Node? node)
    {
        if (node is not State state)
            return;

        base.Value = base.Value == state ? null : base.Value;
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.OnStateRemoved(base.GetChildOrNull<Node>(0));
        base.ChildExitingTree -= this.OnStateRemoved;
        base.ChildEnteredTree -= this.OnStateAdded;

        base.OnDispose();
    }
}
