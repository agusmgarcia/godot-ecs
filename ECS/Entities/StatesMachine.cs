using ECS.Components;
using ECS.Core;
using ECS.Interfaces;
using ECS.Utils;

namespace ECS.Entities;

/// <summary>
/// Entity that acts as a pooled finite state machine; manages a single active <see cref="ECS.Components.State"/> child component and transitions between states each physics frame.
/// </summary>
public abstract partial class StatesMachine : Entity
{
    /// <summary>
    /// Transitions to <typeparamref name="TNewState"/>, pooling the current state and initialising the new one with <paramref name="stateParams"/>; no-ops if the current state blocks transitions via <see cref="ECS.Components.State.ReadyToTransition"/> and <paramref name="force"/> is <c>false</c>.
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
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.Obsolete("", true)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    protected sealed override void AddComponent<TComponent>(TComponent component) =>
        base.AddComponent(component);
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.Obsolete("", true)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    protected sealed override void RemoveComponent<TComponent>(TComponent component) =>
        base.RemoveComponent(component);
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

    /// <inheritdoc/>
    protected override void OnComponentUntracked(IComponent component)
    {
        ElementsPool.Set(component);
        base.OnComponentUntracked(component);
    }
}
