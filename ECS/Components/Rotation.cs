using ECS.Core;
using ECS.Interfaces;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that smoothly rotates the entity to face a world-space target position each physics frame.
/// </summary>
[GlobalClass]
public partial class Rotation : Component<Vector3>
{
    /// <summary>
    /// Rotation speed used when lerping toward the target orientation (degrees per second).
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:°/s")]
    public float AngularSpeed { get; protected set; }

    /// <summary>
    /// World-space position the entity will rotate to face.
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:m")]
    public Vector3 Target { get; set; }

    /// <summary>
    /// Initialises the component with zero rotation and a zeroed target.
    /// </summary>
    public Rotation()
        : base(Vector3.Zero) { }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        this.Target = Vector3.Zero;

        base.ValueChanged += this.OnRotationChanged;
        this.OnRotationChanged(Vector3.Zero);
    }

    /// <inheritdoc/>
    protected override void OnSiblingTracked(IComponent component)
    {
        base.OnSiblingTracked(component);

        if (component is Velocity velocity)
        {
            velocity.ValueChanged += this.OnVelocityChanged;
            this.OnVelocityChanged(velocity.Value);
        }

        // TODO: we could track position component, so we don't have to access it everytime.
    }

    /// <inheritdoc/>
    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);

        // TODO: instead of casting the entity, do base.Entity.Components.GetOrNull<Position>();
        var node = (Node3D)base.Entity!;
        var direction = this.Target - node.GlobalPosition;

        var targetPitch = Mathf.Atan2(-direction.Y, new Vector2(direction.X, direction.Z).Length());
        var targetYaw = Mathf.Atan2(direction.X, direction.Z);
        var targetRoll = 0f;

        var weight = (float)delta * this.AngularSpeed;
        base.Value = new Vector3(
            Mathf.LerpAngle(base.Value.X, targetPitch, weight),
            Mathf.LerpAngle(base.Value.Y, targetYaw, weight),
            Mathf.LerpAngle(base.Value.Z, targetRoll, weight));
    }

    private void OnVelocityChanged(Vector3 velocity)
    {
        // TODO: instead of casting the entity, do base.Entity.Components.GetOrNull<Position>();
        // TODO: instead of casting the entity, do this.Orientation.Z;
        // TODO: make casting optional.
        var node = (Node3D)base.Entity!;
        this.Target = node.GlobalPosition + velocity + node.GlobalBasis.Z;
    }

    private void OnRotationChanged(Vector3 rotation) =>
        ((Node3D)base.Entity!).Rotation = rotation;

    /// <inheritdoc/>
    protected override void OnSiblingUntracked(IComponent component)
    {
        // TODO: we could untrack position component, so we don't have to access it everytime.

        if (component is Velocity velocity)
        {
            this.OnVelocityChanged(velocity.Value);
            velocity.ValueChanged -= this.OnVelocityChanged;
        }

        base.OnSiblingUntracked(component);
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.OnRotationChanged(Vector3.Zero);
        base.ValueChanged -= this.OnRotationChanged;

        this.Target = Vector3.Zero;

        base.OnDispose();
    }
}
