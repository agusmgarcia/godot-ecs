using ECS.Utils;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all components; resolves a reference to the owning <see cref="Entity"/> when it enters the scene tree.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name")]
public partial class Component : Node
{
    /// <summary>
    /// The entity that owns this component; <c>null</c> while the component is outside the scene tree.
    /// </summary>
    protected Entity? Entity { get; private set; }

    private readonly NodesTracker<Node> _childrenTracker = new() { DirectChildren = true };

    /// <summary>
    /// Called once after the component enters the scene tree and internal state is ready.
    /// </summary>
    protected virtual void OnInit()
    {
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

    /// <summary>
    /// Called once before the component exits the scene tree and internal state is torn down.
    /// </summary>
    protected virtual void OnDispose()
    {
        this._childrenTracker.Untrack();
        this._childrenTracker.NodeUntracked -= this.OnSiblingUntracked;
        this._childrenTracker.NodeTracked -= this.OnSiblingTracked;

        this.Entity = null;
    }
}
