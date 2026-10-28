using Godot;

namespace ECS.Components;

/// <summary>
/// Component that drives CharacterBody3D translation and rotation each physics frame.
/// </summary>
[GlobalClass]
public partial class Velocity : Node
{
    /// <summary>
    /// Maximum horizontal speed the entity can reach (m/s).
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:m/s")]
    public float MaxSpeed { get; private set; }

    /// <summary>
    /// Maximum angular speed the entity can reach while rotating (°/s).
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:°/s")]
    public float MaxAngularSpeed { get; private set; }

    private CharacterBody3D? _owner;
    private Vector3 _direction;
    private float _speed;
    private float _acceleration;
    private Vector3 _rotationDirection;
    private float _angularSpeed;
    private float _angularAcceleration;

    /// <summary>
    /// Applies an acceleration impulse in the given direction this physics frame.
    /// </summary>
    public void Accelerate(
        in Vector3 direction,
        float acceleration,
        float angularAcceleration,
        float maxSpeed = -1,
        float maxAngularSpeed = -1)
    {
        this._direction = direction.Normalized();
        this._acceleration = acceleration;
        this._rotationDirection = this._direction;
        this._angularAcceleration = Mathf.DegToRad(angularAcceleration);
        this.MaxSpeed = maxSpeed >= 0 ? maxSpeed : this.MaxSpeed;
        this.MaxAngularSpeed = maxAngularSpeed >= 0 ? maxAngularSpeed : this.MaxAngularSpeed;
    }

    /// <summary>
    /// Applies a deceleration impulse this physics frame, reducing speed toward zero and rotating the owner toward <paramref name="lookAtDirection"/>.
    /// </summary>
    public void Decelerate(float deceleration, in Vector3 lookAtDirection, float angularAcceleration)
    {
        this._acceleration = -deceleration;
        this._rotationDirection = lookAtDirection.Normalized();
        this._angularAcceleration = Mathf.DegToRad(angularAcceleration);
    }

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        this._owner = base.GetOwner<CharacterBody3D>();
        this._direction = Vector3.Zero;
        this._speed = 0f;
        this._acceleration = 0f;
        this._rotationDirection = this._owner.GlobalBasis.Z;
        this._angularSpeed = 0f;
        this._angularAcceleration = 0f;
    }

    /// <inheritdoc/>
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        this.HandleTranslation((float)delta);
        this.HandleRotation((float)delta);
    }

    private void HandleTranslation(float delta)
    {
        this._speed += this._acceleration * delta;
        this._speed = Mathf.Clamp(this._speed, 0f, this.MaxSpeed);
        this._acceleration = 0f;

        if (this._owner == null)
            return;

        this._owner.Velocity = this._direction * this._speed;
        this._owner.MoveAndSlide();
    }

    private void HandleRotation(float delta)
    {
        this._angularSpeed += this._angularAcceleration * delta;
        this._angularSpeed = Mathf.Clamp(this._angularSpeed, 0f, Mathf.DegToRad(this.MaxAngularSpeed));
        this._angularAcceleration = 0f;

        if (this._rotationDirection.IsZeroApprox())
            return;

        if (this._owner == null)
            return;

        var projectedForward = this._owner.GlobalBasis.Z;
        var projectedRotationDirection = this._rotationDirection;

        if (!this._owner.AxisLockAngularX)
        {
            projectedForward.X = 0f;
            projectedRotationDirection.X = 0f;
        }

        if (!this._owner.AxisLockAngularY)
        {
            projectedForward.Y = 0f;
            projectedRotationDirection.Y = 0f;
        }

        if (!this._owner.AxisLockAngularZ)
        {
            projectedForward.Z = 0f;
            projectedRotationDirection.Z = 0f;
        }

        if (projectedForward.IsZeroApprox() || projectedRotationDirection.IsZeroApprox())
            return;

        var angle = projectedForward.AngleTo(projectedRotationDirection);

        if (Mathf.IsZeroApprox(angle))
            return;

        var cross = projectedForward.Cross(projectedRotationDirection);
        var axis = !cross.IsZeroApprox()
            ? cross.Normalized()
            : !this._owner.AxisLockAngularX
                ? this._owner.GlobalBasis.X
                : !this._owner.AxisLockAngularY
                    ? this._owner.GlobalBasis.Y
                    : this._owner.AxisLockAngularZ
                        ? this._owner.GlobalBasis.Z
                        : Vector3.Zero;

        if (this._owner.AxisLockAngularX)
            axis.X = 0f;

        if (this._owner.AxisLockAngularY)
            axis.Y = 0f;

        if (this._owner.AxisLockAngularZ)
            axis.Z = 0f;

        if (axis.IsZeroApprox())
            return;

        this._owner.GlobalRotate(axis.Normalized(), Mathf.Min(this._angularSpeed * delta, angle));
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this._direction = Vector3.Zero;
        this._speed = 0f;
        this._acceleration = 0f;
        this._rotationDirection = Vector3.Zero;
        this._angularSpeed = 0f;
        this._angularAcceleration = 0f;
        this._owner = null;

        base._ExitTree();
    }
}
