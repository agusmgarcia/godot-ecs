using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all components; resolves a reference to the owning <see cref="Entity"/> when it enters the scene tree.
/// </summary>
public abstract partial class Component : Node
{
    /// <summary>
    /// The entity that owns this component; <c>null</c> while the component is outside the scene tree.
    /// </summary>
    protected Entity? Entity { get; private set; }

    public override void _EnterTree()
    {
        base._EnterTree();

        this.Entity = base.GetOwner<Entity>();
    }

    public override void _ExitTree()
    {
        this.Entity = null;

        base._ExitTree();
    }
}