using ECS.Interfaces;
using Godot;

namespace ECS.Components;

/// <summary>
/// ECS-aware wrapper around <see cref="Godot.AnimationPlayer"/> that implements <see cref="IComponent"/>.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name", "Play")]
public partial class AnimationPlayer : Godot.AnimationPlayer, IComponent
{
    /// <summary>
    /// Raised when <see cref="Value"/> is assigned a value that differs from the current one.
    /// </summary>
    public event Action<StringName?>? ValueChanged;

    /// <summary>
    /// The current value.
    /// </summary>
    public StringName? Value
    {
        get;
        protected set
        {
            if (EqualityComparer<StringName>.Default.Equals(field, value))
                return;

            field = value;
            this.ValueChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// Creates a new animation player
    /// </summary>
    public AnimationPlayer() =>
        this.Value = base.CurrentAnimation;

    /// <summary>
    /// Called once after the component enters the scene tree and internal state is ready.
    /// </summary>
    protected virtual void OnInit()
    {
        base.AnimationStarted += this.OnAnimationStarted;
        base.AnimationFinished += this.OnAnimationFinished;
        this.OnAnimationStarted(base.CurrentAnimation);
        base.Stop();
    }

    private void OnAnimationStarted(StringName animationName) =>
        this.Value = animationName;

    private void OnAnimationFinished(StringName animationName) =>
        this.Value = this.Value == animationName ? null : this.Value;

    /// <summary>
    /// Called once before the component exits the scene tree and internal state is torn down.
    /// </summary>
    protected virtual void OnDispose()
    {
        base.Stop();
        this.OnAnimationFinished(base.CurrentAnimation);
        base.AnimationFinished -= this.OnAnimationFinished;
        base.AnimationStarted -= this.OnAnimationStarted;
    }
}
