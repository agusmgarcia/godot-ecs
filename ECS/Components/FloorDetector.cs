using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// // TODO: document this.
/// </summary>
[GlobalClass]
public partial class FloorDetector : Component<bool>
{
    /// <summary>
    /// // TODO: document this.
    /// </summary>
    public FloorDetector()
        : base(false) { }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();
        base.Value = (base.Entity as CharacterBody3D)?.IsOnFloor() ?? false;
    }

    /// <inheritdoc/>
    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);
        base.Value = (base.Entity as CharacterBody3D)?.IsOnFloor() ?? false;
    }

    /// <inheritdoc/>
    protected override void OnDispose()
    {
        base.Value = (base.Entity as CharacterBody3D)?.IsOnFloor() ?? false;
        base.OnDispose();
    }
}
