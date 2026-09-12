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
    /// The entity's current right axis (<see cref="Godot.Basis.X"/>); <see cref="Vector3.Right"/> when outside the scene tree.
    /// </summary>
    public Vector3 Right =>
        (base.Entity as Node3D)?.Basis.X ?? Vector3.Right;

    /// <summary>
    /// The entity's current up axis (<see cref="Godot.Basis.Y"/>); <see cref="Vector3.Up"/> when outside the scene tree.
    /// </summary>
    public Vector3 Up =>
        (base.Entity as Node3D)?.Basis.Y ?? Vector3.Up;

    /// <summary>
    /// The entity's current forward axis (<see cref="Godot.Basis.Z"/>); <see cref="Vector3.Forward"/> when outside the scene tree.
    /// </summary>
    public Vector3 Forward =>
        (base.Entity as Node3D)?.Basis.Z ?? Vector3.Forward;

    /// <summary>
    /// The current Euler rotation in radians; settable from within the assembly only.
    /// </summary>
    public new Vector3 Value
    {
        get => base.Value;
        internal protected set => base.Value = value;
    }

    /// <summary>
    /// When <c>true</c>, the next <see cref="ECS.Core.Component{TValue}.ValueChanged"/> callback originates from a scene-tree transform notification and must not write back to the entity.
    /// </summary>
    internal bool NotificationFromParent { private get; set; }

    private Position? _position;
    private Velocity? _velocity;
    private Vector3 _target;

    /// <summary>
    /// Initialises the component with zero rotation and a zeroed target.
    /// </summary>
    public Rotation()
        : base(Vector3.Zero) { }

    /// <summary>
    /// Sets the world-space <paramref name="target"/> position the entity will rotate to face each physics frame.
    /// </summary>
    public void LookAt(in Vector3 target) =>
        this._target = target;

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        this._position = null;
        this._velocity = null;
        this._target = Vector3.Zero;

        base.ValueChanged += this.OnRotationChanged;
        this.OnRotationChanged((base.Entity as Node3D)?.Rotation ?? Vector3.Zero);
    }

    /// <inheritdoc/>
    protected override void OnSiblingTracked(IComponent component)
    {
        base.OnSiblingTracked(component);

        if (component is Velocity velocity && this._velocity == null)
        {
            this._velocity = velocity;
            this._velocity.ValueChanged += this.OnVelocityChanged;
            this.OnVelocityChanged(this._velocity.Value);
        }

        if (component is Position position && this._position == null)
        {
            this._position = position;
            this._position.ValueChanged += this.OnPositionChanged;
            this.OnPositionChanged(this._position.Value);
        }
    }

    /// <inheritdoc/>
    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);

        var direction = this._target - (this._position?.Value ?? Vector3.Zero);

        var targetPitch = Mathf.Atan2(-direction.Y, new Vector2(direction.X, direction.Z).Length());
        var targetYaw = Mathf.Atan2(direction.X, direction.Z);
        var targetRoll = 0f;

        var weight = (float)delta * this.AngularSpeed;
        base.Value = new Vector3(
            Mathf.LerpAngle(base.Value.X, targetPitch, weight),
            Mathf.LerpAngle(base.Value.Y, targetYaw, weight),
            Mathf.LerpAngle(base.Value.Z, targetRoll, weight));
    }

    private void OnPositionChanged(Vector3 position) =>
        this._target = position + (this._velocity?.Value ?? Vector3.Zero) + this.Forward;

    private void OnVelocityChanged(Vector3 velocity) =>
        this._target = (this._position?.Value ?? Vector3.Zero) + velocity + this.Forward;

    private void OnRotationChanged(Vector3 rotation)
    {
        if (!this.NotificationFromParent)
            (base.Entity as Node3D)?.Rotation = rotation;
    }

    /// <inheritdoc/>
    protected override void OnSiblingUntracked(IComponent component)
    {
        if (component is Position position && this._position == position)
        {
            this.OnPositionChanged(this._position.Value);
            this._position.ValueChanged -= this.OnPositionChanged;
            this._position = null;
        }

        if (component is Velocity velocity && this._velocity == velocity)
        {
            this.OnVelocityChanged(this._velocity.Value);
            this._velocity.ValueChanged -= this.OnVelocityChanged;
            this._velocity = null;
        }

        base.OnSiblingUntracked(component);
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.OnRotationChanged((base.Entity as Node3D)?.Rotation ?? Vector3.Zero);
        base.ValueChanged -= this.OnRotationChanged;

        this._target = Vector3.Zero;
        this._velocity = null;
        this._position = null;

        base.OnDispose();
    }
}
