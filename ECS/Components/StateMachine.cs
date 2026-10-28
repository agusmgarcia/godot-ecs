using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that acts as a pooled finite state machine; manages a single active <see cref="ECS.Components.State"/> child component and transitions between states each physics frame.
/// </summary>
[GlobalClass]
public partial class StateMachine : Node
{
    /// <summary>
    /// Fired whenever the active state changes, passing the newly active state or null when cleared.
    /// </summary>
    public event Action<State?>? StateChanged;

    /// <summary>
    /// The currently active state child, or null when no state is set.
    /// </summary>
    public State? State
    {
        get;
        private set
        {
            field = value;
            this.StateChanged?.Invoke(field);
        }
    }

    private readonly Queue<ValueTuple<State?, ValueType, bool>> _pendingStates = [];

    private bool _isTransitioning;

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        base.ChildExitingTree += this.OnStateRemoved;
        base.ChildEnteredTree += this.OnStateAdded;
        this.OnStateAdded(base.GetChildOrNull<State>(0));
    }

    private void OnStateAdded(Node? node)
    {
        if (node == null)
            return;

        if (node is not State state)
            throw new InvalidOperationException($"The node '{node.Name}' should be of type '{typeof(State).Name}'");

        this.State = state;
    }

    /// <summary>
    /// Transitions to <typeparamref name="TNewState"/>, pooling the current state and initialising the new one with <paramref name="stateParams"/>; no-ops if the current state blocks transitions via <see cref="ECS.Components.State._ReadyToTransition"/> and <paramref name="force"/> is <c>false</c>.
    /// </summary>
    protected void SetState<TNewState, TStateParams>(in TStateParams stateParams, bool force = false)
        where TNewState : State<TStateParams>, new()
        where TStateParams : struct
    {
        var newState = ElementsPool.GetOrCreate<TNewState>();
        newState.Name = typeof(TNewState).Name;
        newState._CopyStateParams(stateParams);

        this._pendingStates.Enqueue((newState, stateParams, force));
        this.FlushPendingStates();
    }

    private void FlushPendingStates()
    {
        if (this._isTransitioning)
            return;

        this._isTransitioning = true;

        while (this._pendingStates.TryDequeue(out var nextStateTuple))
        {
            var (nextState, nextStateParams, nextStateForce) = nextStateTuple;
            if (!nextStateForce && this.State != null && !this.State._ReadyToTransition())
            {
                if (nextState != null)
                    ElementsPool.Set(nextState);

                continue;
            }

            if (this.State != null && this.State.GetType() == nextState?.GetType())
            {
                this.State._CopyStateParams(nextStateParams);
                ElementsPool.Set(nextState);
                continue;
            }

            if (this.State != null)
            {
                var currentState = this.State;
                base.RemoveChild(currentState);
                ElementsPool.Set(currentState);
            }

            if (nextState != null)
                base.AddChild(nextState);
        }

        this._isTransitioning = false;
    }

    /// <summary>
    /// Clears the current state; no-ops if the current state blocks transitions via <see cref="ECS.Components.State._ReadyToTransition"/> and <paramref name="force"/> is <c>false</c>.
    /// </summary>
    protected void ClearState(bool force = false)
    {
        this._pendingStates.Enqueue((null, 0, force));
        this.FlushPendingStates();
    }

    private void OnStateRemoved(Node? node)
    {
        if (node == null)
            return;

        if (node is not State state)
            throw new InvalidOperationException($"The node '{node.Name}' should be of type '{typeof(State).Name}'");

        this.State = this.State == state ? null : this.State;
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this.OnStateRemoved(base.GetChildOrNull<State>(0));
        base.ChildExitingTree -= this.OnStateRemoved;
        base.ChildEnteredTree -= this.OnStateAdded;

        base._ExitTree();
    }
}
