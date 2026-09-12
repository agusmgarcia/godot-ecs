using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// // TODO: document this.
/// </summary>
[GlobalClass]
public partial class Scale : Component<Vector3>
{
    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public Scale()
        : base(Vector3.One) { }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        base.ValueChanged += this.OnScaleChanged;
        this.OnScaleChanged((base.Entity as Node3D)?.Scale ?? Vector3.Zero);
    }

    private void OnScaleChanged(Vector3 scale) =>
        (base.Entity as Node3D)?.Scale = scale;

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.OnScaleChanged((base.Entity as Node3D)?.Scale ?? Vector3.Zero);
        base.ValueChanged -= this.OnScaleChanged;

        base.OnDispose();
    }
}
