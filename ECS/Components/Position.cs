using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that mirrors the owning entity's world position as a <see cref="Vector3"/> value; writes propagate to the entity's <see cref="Godot.Node3D.Position"/> and external scene-tree transform changes are pushed back via the entity's local-transform notification.
/// </summary>
[GlobalClass]
public partial class Position : Component<Vector3>
{
    /// <summary>
    /// The current world position; settable from within the assembly only.
    /// </summary>
    public new Vector3 Value
    {
        get => base.Value;
        internal protected set => base.Value = value;
    }

    /// <summary>
    /// When <c>true</c>, the next <see cref="ECS.Core.Component{TValue}.ValueChanged"/> callback is the result of a scene-tree transform notification and must not write back to the entity.
    /// </summary>
    internal bool NotificationFromParent { private get; set; }

    /// <summary>
    /// Initialises the component with a position of <see cref="Vector3.Zero"/>.
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
