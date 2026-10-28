using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// AnimationPlayer that smoothly blends a sibling Model's position and rotation per animation.
/// Position is linearly interpolated; rotation follows the shortest arc via quaternion Slerp.
/// Both transforms reach their target in exactly <see cref="Duration"/> seconds.
/// </summary>
[GlobalClass]
public partial class AutoTransformAnimationPlayer : AnimationPlayer
{
    /// <summary>
    /// Time in seconds for the model to fully blend from its current transform to the target
    /// transform when an animation starts or finishes. A value of <c>0</c> snaps instantly.
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:s")]
    public float Duration { get; private set; }

    private ModelHandler? _modelHandler;

    private Node3D? _model;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private Quaternion _startRotation;
    private Quaternion _targetRotation;
    private float _elapsed;

    /// <summary>
    /// Initialises the player and subscribes to the sibling <see cref="ModelHandler"/>.
    /// </summary>
    public AutoTransformAnimationPlayer()
    {
        this.TrackSiblings<ModelHandler>(
            this.OnModelHandlerTracked,
            this.OnModelHandlerUntracked,
            unique: true);
    }

    /// <summary>
    /// Returns the target model position offset for the given animation name.
    /// </summary>
    protected virtual Vector3 GetTargetPosition(string animationName) =>
        Vector3.Zero;

    /// <summary>
    /// Returns the target model rotation (as Euler angles in radians) for the given animation name.
    /// </summary>
    protected virtual Vector3 GetTargetRotation(string animationName) =>
        Vector3.Zero;

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        base.AnimationFinished += this.OnAnimationFinished;
        base.AnimationStarted += this.OnAnimationStarted;
        this.OnAnimationStarted(base.CurrentAnimation);
    }

    private void OnModelHandlerTracked(ModelHandler modelHandler)
    {
        this._modelHandler = modelHandler;
        this._modelHandler.ModelChanged += this.OnModelChanged;
        this.OnModelChanged(this._modelHandler.Model);
    }

    private void OnAnimationStarted(StringName animationName)
    {
        this._startPosition = this._model?.Position ?? Vector3.Zero;
        this._targetPosition = !string.IsNullOrEmpty(animationName)
            ? this.GetTargetPosition(animationName)
            : Vector3.Zero;

        this._startRotation = this._model?.Quaternion ?? Quaternion.Identity;
        this._targetRotation = !string.IsNullOrEmpty(animationName)
            ? Basis.FromEuler(this.GetTargetRotation(animationName)).GetRotationQuaternion()
            : Quaternion.Identity;

        this._elapsed = 0f;
    }

    private void OnModelChanged(Node3D? model)
    {
        this._model = model;
        if (this._model != null)
            base.RootNode = base.GetPathTo(this._model);
    }

    /// <inheritdoc/>
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (this._model == null)
            return;

        this._elapsed += (float)delta;

        float weight = this.Duration > 0f
            ? Mathf.Clamp(this._elapsed / this.Duration, 0f, 1f)
            : 1f;

        this._model.Position = this._startPosition.Lerp(this._targetPosition, weight);
        this._model.Quaternion = this._startRotation.Slerp(this._targetRotation, weight);
    }

    private void OnAnimationFinished(StringName animationName)
    {
        this._elapsed = 0f;

        this._targetRotation = Quaternion.Identity;
        this._startRotation = this._model?.Quaternion ?? Quaternion.Identity;

        this._targetPosition = Vector3.Zero;
        this._startPosition = this._model?.Position ?? Vector3.Zero;
    }

    private void OnModelHandlerUntracked(ModelHandler modelHandler)
    {
        this.OnModelChanged(modelHandler.Model);
        modelHandler.ModelChanged -= this.OnModelChanged;
        this._modelHandler = null;
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this.OnAnimationFinished(base.CurrentAnimation);
        base.AnimationFinished -= this.OnAnimationFinished;
        base.AnimationStarted -= this.OnAnimationStarted;

        base._ExitTree();
    }
}