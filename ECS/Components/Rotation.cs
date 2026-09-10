using ECS.Core;
using ECS.Utils;
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

    private readonly NodesTracker<Velocity> _velocityTracker = new();

    /// <summary>
    /// Initialises the component with zero rotation and a zeroed target.
    /// </summary>
    public Rotation()
        : base(Vector3.Zero) { }

    public override void _EnterTree()
    {
        base._EnterTree();

        this.Target = Vector3.Zero;

        this._velocityTracker.NodeTracked += this.OnVelocityTracked;
        this._velocityTracker.NodeUntracked += this.OnVelocityUntracked;
        this._velocityTracker.Track(base.Entity!);

        base.ValueChanged += this.OnRotationChanged;
        this.OnRotationChanged(Vector3.Zero);
    }

    private void OnVelocityTracked(Velocity velocity)
    {
        velocity.ValueChanged += this.OnVelocityChanged;
        this.OnVelocityChanged(velocity.Value);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        var direction = this.Target - base.Entity!.GlobalPosition;

        var targetPitch = Mathf.Atan2(-direction.Y, new Vector2(direction.X, direction.Z).Length());
        var targetYaw = Mathf.Atan2(direction.X, direction.Z);
        var targetRoll = 0f;

        var weight = (float)delta * this.AngularSpeed;
        base.Value = new Vector3(
            Mathf.LerpAngle(base.Value.X, targetPitch, weight),
            Mathf.LerpAngle(base.Value.Y, targetYaw, weight),
            Mathf.LerpAngle(base.Value.Z, targetRoll, weight));
    }

    private void OnVelocityChanged(Vector3 velocity) =>
        this.Target = base.Entity!.GlobalPosition + velocity + base.Entity.GlobalBasis.Z;

    private void OnRotationChanged(Vector3 rotation) =>
        base.Entity!.Rotation = rotation;

    private void OnVelocityUntracked(Velocity velocity)
    {
        this.OnVelocityChanged(velocity.Value);
        velocity.ValueChanged -= this.OnVelocityChanged;
    }

    public override void _ExitTree()
    {
        this.OnRotationChanged(Vector3.Zero);
        base.ValueChanged -= this.OnRotationChanged;

        this._velocityTracker.Untrack();
        this._velocityTracker.NodeUntracked -= this.OnVelocityUntracked;
        this._velocityTracker.NodeTracked -= this.OnVelocityTracked;

        this.Target = Vector3.Zero;

        base._ExitTree();
    }
}
