using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that mirrors the entity's 3D scale as a <see cref="Vector3"/> value; writing <see cref="ECS.Core.Component{TValue}.Value"/> immediately updates the entity's scale. Defaults to <see cref="Vector3.One"/>.
/// </summary>
[GlobalClass]
public partial class Scale : Component<Vector3>
{
    /// <summary>
    /// The current value.
    /// </summary>
    public new Vector3 Value
    {
        get => base.Value;
        set => base.Value = value;
    }

    /// <summary>
    /// Initialises the component with a scale of <see cref="Vector3.One"/>.
    /// </summary>
    public Scale()
        : base(Vector3.One) { }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        base.ValueChanged += this.OnScaleChanged;
        this.OnScaleChanged((base.Owner as Node3D)?.Scale ?? Vector3.Zero);
    }

    private void OnScaleChanged(Vector3 scale) =>
        (base.Owner as Node3D)?.Scale = scale;

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.OnScaleChanged((base.Owner as Node3D)?.Scale ?? Vector3.Zero);
        base.ValueChanged -= this.OnScaleChanged;

        base.OnDispose();
    }
}
