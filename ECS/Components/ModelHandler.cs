using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// Abstract Node3D that manages swappable 3D model children, keeping exactly one visible; auto-adjusts position and scale via a sibling <see cref="ECS.Components.Height"/> component.
/// </summary>
[GlobalClass]
public partial class ModelHandler : Node3D
{
    /// <summary>
    /// Fired whenever the active model changes, passing the newly visible <see cref="Node3D"/> or <c>null</c> when no model is selected.
    /// </summary>
    public event Action<Node3D?>? ModelChanged;

    /// <summary>
    /// The currently active model child, resolved from <see cref="ModelType"/>; <c>null</c> when no model type is selected.
    /// </summary>
    public Node3D? Model =>
        this._models.GetValueOrDefault(this.ModelType);

    /// <summary>
    /// Key identifying the active child model; setting it switches visibility and fires <see cref="ModelChanged"/>. Defaults to <see cref="string.Empty"/> (no model selected).
    /// </summary>
    [Export]
    public string ModelType
    {
        get;
        private set
        {
            field = value;
            this.ModelChanged?.Invoke(this.Model);
        }
    } = string.Empty;

    /// <summary>
    /// When <c>true</c>, the node's vertical position is not adjusted relative to the sibling <see cref="ECS.Components.Height"/>.
    /// </summary>
    [ExportGroup("Position", "Position")]
    [Export]
    public bool PositionDisabled { get; private set; }

    /// <summary>
    /// When <c>true</c>, the node's scale is not adjusted to match the sibling <see cref="ECS.Components.Height"/>.
    /// </summary>
    [ExportGroup("Height", "Height")]
    [Export]
    public bool HeightDisabled { get; private set; }

    /// <summary>
    /// Design-time height (metres) used as the scale denominator against the sibling <see cref="ECS.Components.Height"/>; set it to the height at which the model was authored.
    /// </summary>
    [ExportGroup("Height", "Height")]
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:m")]
    public float HeightDefaultValue { get; private set; }

    private readonly Dictionary<string, Node3D?> _models = new()
    {
        [string.Empty] = null,
    };

    private Height? _height;

    /// <summary>
    /// Registers trackers for the sibling <see cref="ECS.Components.Height"/> and for direct <see cref="Node3D"/> children that represent swappable models.
    /// </summary>
    public ModelHandler()
    {
        this.TrackSiblings<Height>(
            this.OnHeightTracked,
            this.OnHeightUntracked,
            unique: true);

        this.TrackChildren<Node3D>(
            this.OnModelTracked,
            this.OnModelUntracked,
            direct: true);
    }

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        this.ModelChanged += this.OnModelChanged;
        this.OnModelChanged(this.Model);
    }

    private void OnHeightTracked(Height height)
    {
        this._height = height;

        if (!this.PositionDisabled)
            base.Position = new Vector3(0f, -this._height.Value / 2f, 0f);

        if (!this.HeightDisabled)
            base.Scale = Vector3.One * (this._height.Value / this.HeightDefaultValue);
    }

    private void OnModelTracked(Node3D model)
    {
        var modelType = this.GetModelType(model);
        this._models.Add(modelType, model);
        this.ModelType = modelType;
    }

    /// <summary>
    /// Gets the model type from the model passed by args.
    /// </summary>
    protected virtual string GetModelType(Node3D model) =>
        model.Name.ToString().Replace("Model", "");

    private void OnModelChanged(Node3D? model)
    {
        foreach (var cachedModel in this._models.Values)
            cachedModel?.Visible = cachedModel == model;
    }

    private void OnModelUntracked(Node3D model)
    {
        var modelType = this.GetModelType(model);
        this.ModelType = string.Empty;
        this._models.Remove(modelType);
    }

    private void OnHeightUntracked(Height height)
    {
        if (!this.HeightDisabled)
            base.Scale = Vector3.One;

        if (!this.PositionDisabled)
            base.Position = Vector3.Zero;

        this._height = null;
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this.OnModelChanged(this.Model);
        this.ModelChanged -= this.OnModelChanged;

        base._ExitTree();
    }
}
