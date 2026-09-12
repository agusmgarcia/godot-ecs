using ECS.Components;
using ECS.Core;
using ECS.Interfaces;
using ECS.Utils;
using Godot;

namespace ECS.Entities;

/// <summary>
/// Generic finite state machine component that pools states and processes queued transitions each physics frame.
/// </summary>
[GlobalClass]
public partial class StatesMachine : Entity
{
    /// <summary>
    /// Enqueues a transition to <typeparamref name="TNewState"/>; the transition is applied on the next physics frame.
    /// </summary>
    protected void SetState<TNewState, TStateParams>(in TStateParams stateParams, bool force = false)
        where TNewState : State<TStateParams>, new()
        where TStateParams : struct
    {
        var currentState = base.Components.GetOrNull<State>();
        if (!force && currentState != null && !currentState.ReadyToTransition())
            return;

        if (currentState != null && currentState.GetType() == typeof(TNewState))
        {
            currentState.CopyStateParams(stateParams);
            return;
        }

        if (currentState != null)
            this.RemoveComponent(currentState);

        var newState = ElementsPool.GetOrCreate<TNewState>();
        newState.StateParams = stateParams;
        this.AddComponent(newState);
    }

    /// <inheritdoc/>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    protected sealed override void AddComponent<TComponent>(TComponent component) =>
        base.AddComponent(component);

    /// <inheritdoc/>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    protected sealed override void RemoveComponent<TComponent>(TComponent component) =>
        base.RemoveComponent(component);

    /// <inheritdoc/>
    protected override void OnComponentUntracked(IComponent component)
    {
        ElementsPool.Set(component);
        base.OnComponentUntracked(component);
    }
}
