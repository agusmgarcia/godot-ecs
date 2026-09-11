using ECS.Core;
using ECS.Interfaces;
using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// Generic finite state machine component that pools states and processes queued transitions each physics frame.
/// </summary>
[GlobalClass]
public partial class StatesMachine : Component<StatesMachine.BaseState?>
{
    private readonly Queue<ValueTuple<BaseState, bool>> _newStates = [];

    /// <summary>
    /// Initialises the state machine with no active state.
    /// </summary>
    protected StatesMachine()
        : base(null) { }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        foreach (var (newState, _) in this._newStates)
        {
            newState.Entity = null!;
            ElementsPool.Set(newState);
        }

        this._newStates.Clear();

        if (this.Value != null)
        {
            StatesMachine.DisposeState(this.Value);
            this.Value = null;
        }
    }

    /// <summary>
    /// Enqueues a transition to <typeparamref name="TNewState"/>; the transition is applied on the next physics frame.
    /// </summary>
    protected void SetState<TNewState, TStateParams>(in TStateParams stateParams, bool force = false)
        where TNewState : BaseState<TStateParams>, new()
        where TStateParams : struct
    {
        if (base.Entity == null)
            throw new InvalidOperationException($"Component is not part of the tree");

        var state = ElementsPool.GetOrCreate<TNewState>();
        state.Entity = base.Entity;
        state.StateParams = stateParams;
        this._newStates.Enqueue((state, force));
    }

    /// <inheritdoc/>
    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);

        while (this._newStates.TryDequeue(out var item))
        {
            var (newState, force) = item;

            if (!force && this.Value != null && !this.Value.ReadyToTransition())
            {
                newState.Entity = null!;
                ElementsPool.Set(newState);
                continue;
            }

            if (this.Value != null && this.Value.GetType() == newState.GetType())
            {
                this.Value.Entity = newState.Entity;
                this.Value.CopyParamsFrom(newState);

                newState.Entity = null!;
                ElementsPool.Set(newState);
            }
            else
            {
                if (this.Value != null)
                    StatesMachine.DisposeState(this.Value);

                newState.OnInit();
                this.Value = newState;
            }
        }

        this.Value?.OnUpdate(delta);
    }

    private static void DisposeState(BaseState state)
    {
        state.OnDispose();
        state.Entity = null!;
        ElementsPool.Set(state);
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        if (this.Value != null)
        {
            StatesMachine.DisposeState(this.Value);
            this.Value = null;
        }

        foreach (var (newState, _) in this._newStates)
        {
            newState.Entity = null!;
            ElementsPool.Set(newState);
        }

        this._newStates.Clear();

        base.OnDispose();
    }

    /// <summary>
    /// Base class for all states managed by <see cref="StatesMachine"/>.
    /// </summary>
    public abstract class BaseState
    {
        /// <summary>
        /// The entity this state is currently operating on.
        /// </summary>
        public IEntity Entity { get; internal set; } = null!;

        private readonly NodesTracker<IComponent> _siblingsTracker = new() { DirectChildren = true };

        /// <summary>
        /// Called once when this state becomes the active state.
        /// </summary>
        internal protected virtual void OnInit()
        {
            this._siblingsTracker.NodeTracked += this.OnSiblingTracked;
            this._siblingsTracker.NodeUntracked += this.OnSiblingUntracked;
            this._siblingsTracker.Track((global::Godot.Node)(object)this.Entity);
        }

        /// <summary>
        /// Called when a sibling component is added to the owning entity.
        /// </summary>
        protected virtual void OnSiblingTracked(IComponent component) { }

        /// <summary>
        /// Called every physics frame while this state is active.
        /// </summary>
        internal protected virtual void OnUpdate(double delta) { }

        /// <summary>
        /// Returns <c>false</c> to block non-forced transitions away from this state.
        /// </summary>
        internal protected virtual bool ReadyToTransition() => true;

        internal virtual void CopyParamsFrom(BaseState source) { }

        /// <summary>
        /// Called when a sibling component is removed from the owning entity.
        /// </summary>
        protected virtual void OnSiblingUntracked(IComponent component) { }

        /// <summary>
        /// Called once when this state is replaced by another state.
        /// </summary>
        internal protected virtual void OnDispose()
        {
            this._siblingsTracker.Untrack();
            this._siblingsTracker.NodeUntracked -= this.OnSiblingUntracked;
            this._siblingsTracker.NodeTracked -= this.OnSiblingTracked;
        }
    }

    /// <summary>
    /// Base class for states that carry a <typeparamref name="TStateParams"/> struct passed on each transition.
    /// </summary>
    public abstract class BaseState<TStateParams> : BaseState
        where TStateParams : struct
    {
        /// <summary>
        /// Parameters supplied by the last <see cref="StatesMachine.SetState{TNewState, TStateParams}"/> call.
        /// </summary>
        public TStateParams StateParams { get; internal set; } = default;

        internal sealed override void CopyParamsFrom(BaseState source) =>
            this.StateParams = ((BaseState<TStateParams>)source).StateParams;
    }
}
