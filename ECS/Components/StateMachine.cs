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

    private readonly Queue<ValueTuple<State, ValueType>> _pendingStates = [];

    private bool _isTransitioning;

    /// <summary>
    /// Initialises the machine and starts tracking direct child nodes.
    /// </summary>
    public StateMachine()
    {
        this.TrackChildren<Node>(
            this.OnChildTracked,
            this.OnChildUntracked,
            direct: true,
            unique: true);
    }

    /// <summary>
    /// Transitions to <typeparamref name="TNewState"/>, pooling the current state and initialising the new one with <paramref name="stateParams"/>.
    /// </summary>
    public void SetState<TNewState, TStateParams>(in TStateParams stateParams)
        where TNewState : State<TStateParams>, new()
        where TStateParams : struct
    {
        var newState = ElementsPool.GetOrCreate<TNewState>();
        newState.Name = typeof(TNewState).Name;
        newState._CopyStateParams(stateParams);

        this._pendingStates.Enqueue((newState, stateParams));

        if (this._isTransitioning)
            return;

        this._isTransitioning = true;

        while (this._pendingStates.TryDequeue(out var nextStateTuple))
        {
            var (nextState, nextStateParams) = nextStateTuple;

            if (this.State != null)
            {
                if (!this.State._ReadyToTransition(nextState))
                {
                    ElementsPool.Set(nextState);
                    continue;
                }

                if (this.State.GetType() == nextState.GetType())
                {
                    ElementsPool.Set(nextState);
                    this.State._CopyStateParams(nextStateParams);
                    continue;
                }

                ElementsPool.Set(this.State);
                base.RemoveChild(this.State);
            }

            base.AddChild(nextState);
        }

        this._isTransitioning = false;
    }

    private void OnChildTracked(Node node)
    {
        if (node is not State state)
            throw new InvalidOperationException($"The node '{node.Name}' should be of type '{typeof(State).Name}'");

        this.State = state;
    }

    private void OnChildUntracked(Node node)
    {
        if (node is not State state)
            throw new InvalidOperationException($"The node '{node.Name}' should be of type '{typeof(State).Name}'");

        this.State = this.State == state ? null : this.State;
    }
}
