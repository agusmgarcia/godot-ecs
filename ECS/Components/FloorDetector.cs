using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that tracks whether the owning <see cref="Godot.CharacterBody3D"/> entity is currently on the floor; exposes the result as a <c>bool</c> value updated every physics frame.
/// </summary>
[GlobalClass]
public partial class FloorDetector : Component<bool>
{
    /// <summary>
    /// Initialises the component with an initial value of <c>false</c>.
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
