using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that drives 3D physics velocity, applying gravity, air friction, and a max speed limit.
/// </summary>
[GlobalClass]
public partial class Velocity : Component<Vector3>
{
    /// <summary>
    /// Downward acceleration applied to the entity while airborne (m/s²).
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:m/s²")]
    public float Gravity { get; protected set; } =
        (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

    /// <summary>
    /// Horizontal speed loss per second applied while the entity is airborne (m/s²).
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:m/s²")]
    public float AirFriction { get; protected set; } =
        (float)ProjectSettings.GetSetting("physics/3d/default_linear_damp");

    /// <summary>
    /// Maximum horizontal speed the entity can reach (m/s).
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:m/s")]
    public float MaxSpeed { get; protected set; }

    /// <summary>
    /// Initialises the component with zero initial velocity.
    /// </summary>
    public Velocity()
        : base(Vector3.Zero) { }

    private Vector3 _direction;
    private float _speed;
    private float _pendingSpeedDelta;

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        this._direction = Vector3.Zero;
        this._speed = 0;
        this._pendingSpeedDelta = 0;

        base.ValueChanged += this.OnVelocityChanged;
        this.OnVelocityChanged(Vector3.Zero);
    }

    /// <summary>
    /// Applies an acceleration impulse in the given direction this physics frame.
    /// </summary>
    public void Accelerate(in Vector3 direction, float acceleration)
    {
        this._direction = direction.Normalized();
        this._pendingSpeedDelta = acceleration;
    }

    /// <summary>
    /// Applies a deceleration impulse this physics frame, reducing speed toward zero.
    /// </summary>
    public void Decelerate(float deceleration) =>
        this._pendingSpeedDelta = -deceleration;

    /// <inheritdoc/>
    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);

        // TODO: check if it makes sense to have a component that tracks whether an entity is on the floor or not.
        // if not, the casting should be optional base.Entity! as CharacterBody3D.
        var body = (CharacterBody3D)base.Entity!;

        this._speed += this._pendingSpeedDelta * (float)delta;
        this._speed = Mathf.Clamp(this._speed, 0f, this.MaxSpeed);
        this._pendingSpeedDelta = 0f;

        if (!body.IsOnFloor())
            this._speed = Mathf.Max(this._speed - this.AirFriction * (float)delta, 0f);

        var velocity = this._direction * this._speed;
        velocity.Y = body.IsOnFloor()
            ? (velocity.Y <= 0 ? 0 : velocity.Y)
            : (base.Value.Y - this.Gravity * (float)delta);

        base.Value = velocity;
    }

    private void OnVelocityChanged(Vector3 velocity)
    {
        var characterBody3D = base.Entity! as CharacterBody3D;
        characterBody3D?.Velocity = velocity;
        characterBody3D?.MoveAndSlide();
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.OnVelocityChanged(Vector3.Zero);
        base.ValueChanged -= this.OnVelocityChanged;

        this._pendingSpeedDelta = 0;
        this._speed = 0;
        this._direction = Vector3.Zero;

        base.OnDispose();
    }
}
