using ECS.Core;
using ECS.Interfaces;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that drives 3D physics velocity, applying gravity, air friction, and a max speed limit.
/// </summary>
public abstract partial class Velocity : Component<Vector3>
{
    /// <summary>
    /// Maximum horizontal speed the entity can reach (m/s).
    /// </summary>
    public float MaxSpeed { get; protected set; }

    private FloorDetector? _floorDetector;

    private Vector3 _direction;
    private float _pendingSpeedDelta;
    private float _speed;
    private float _gravity;
    private float _airFriction;

    /// <summary>
    /// Initialises the component with zero initial velocity.
    /// </summary>
    public Velocity()
        : base(Vector3.Zero) { }

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
    protected override void OnInit()
    {
        base.OnInit();

        this._direction = Vector3.Zero;
        this._pendingSpeedDelta = 0;
        this._speed = 0;
        this._gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
        this._airFriction = (float)ProjectSettings.GetSetting("physics/3d/default_linear_damp");

        base.ValueChanged += this.OnVelocityChanged;
        this.OnVelocityChanged((base.Owner as CharacterBody3D)?.Velocity ?? Vector3.Zero);
    }

    /// <inheritdoc/>
    protected override void OnSiblingComponentTracked(IComponent component)
    {
        base.OnSiblingComponentTracked(component);

        if (component is FloorDetector floorDetector && this._floorDetector == null)
            this._floorDetector = floorDetector;
    }

    /// <inheritdoc/>
    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);

        this._speed += this._pendingSpeedDelta * (float)delta;
        this._speed = Mathf.Clamp(this._speed, 0f, this.MaxSpeed);
        this._pendingSpeedDelta = 0f;

        if (!(this._floorDetector?.Value ?? false))
            this._speed = Mathf.Max(this._speed - this._airFriction * (float)delta, 0f);

        var velocity = this._direction * this._speed;
        velocity.Y = (this._floorDetector?.Value ?? false)
            ? (velocity.Y <= 0 ? 0 : velocity.Y)
            : (base.Value.Y - this._gravity * (float)delta);

        base.Value = velocity;
    }

    private void OnVelocityChanged(Vector3 velocity)
    {
        var characterBody3D = base.Owner as CharacterBody3D;
        characterBody3D?.Velocity = velocity;
        characterBody3D?.MoveAndSlide();
    }

    /// <inheritdoc/>
    protected override void OnSiblingComponentUntracked(IComponent component)
    {
        if (component is FloorDetector floorDetector && this._floorDetector == floorDetector)
            this._floorDetector = null;

        base.OnSiblingComponentUntracked(component);
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.OnVelocityChanged((base.Owner as CharacterBody3D)?.Velocity ?? Vector3.Zero);
        base.ValueChanged -= this.OnVelocityChanged;

        this._airFriction = 0;
        this._gravity = 0;
        this._speed = 0;
        this._pendingSpeedDelta = 0;
        this._direction = Vector3.Zero;

        base.OnDispose();
    }
}
