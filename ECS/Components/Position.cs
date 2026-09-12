using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// // TODO: document this.
/// </summary>
[GlobalClass]
public partial class Position : Component<Vector3>
{
    /// <inheritdoc/>
    public new Vector3 Value
    {
        get => base.Value;
        internal protected set => base.Value = value;
    }

    internal bool NotificationFromParent { private get; set; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public Position()
        : base(Vector3.Zero) { }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();

        base.ValueChanged += this.OnPositionChanged;
        this.OnPositionChanged((base.Entity as Node3D)?.Position ?? Vector3.Zero);
    }

    private void OnPositionChanged(Vector3 position)
    {
        if (!this.NotificationFromParent)
            (base.Entity as Node3D)?.Position = position;
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        this.OnPositionChanged((base.Entity as Node3D)?.Position ?? Vector3.Zero);
        base.ValueChanged -= this.OnPositionChanged;

        base.OnDispose();
    }
}
