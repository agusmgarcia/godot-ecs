using ECS.Utils;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all components; resolves a reference to the owning <see cref="Entity"/> when it enters the scene tree.
/// </summary>
[GlobalClass]
public partial class Component : Node
{
    /// <summary>
    /// The entity that owns this component; <c>null</c> while the component is outside the scene tree.
    /// </summary>
    protected Entity? Entity { get; private set; }

    private readonly NodesTracker<Node> _childrenTracker = new() { DirectChildren = true };

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        this.Entity = base.GetOwner<Entity>();

        this._childrenTracker.NodeTracked += this.OnSiblingTracked;
        this._childrenTracker.NodeUntracked += this.OnSiblingUntracked;
        this._childrenTracker.Track(this.Entity);
    }

    /// <summary>
    /// Called when a sibling node is added to the owning <see cref="Entity"/>.
    /// </summary>
    protected virtual void OnSiblingTracked(Node node) { }

    /// <summary>
    /// Called when a sibling node is removed from the owning <see cref="Entity"/>.
    /// </summary>
    protected virtual void OnSiblingUntracked(Node node) { }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this._childrenTracker.Untrack();
        this._childrenTracker.NodeUntracked -= this.OnSiblingUntracked;
        this._childrenTracker.NodeTracked -= this.OnSiblingTracked;

        this.Entity = null;

        base._ExitTree();
    }
}
