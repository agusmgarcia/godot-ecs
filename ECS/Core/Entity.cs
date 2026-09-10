using ECS.Utils;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all game entities; extends <see cref="Godot.CharacterBody3D"/> and exposes a live typed set of its child nodes.
/// </summary>
[HideInheritedMembers("_PhysicsProcess", "_EnterTree", "_ExitTree", "Name")]
public abstract partial class Entity : CharacterBody3D
{
    /// <summary>
    /// Live typed set of all descendant nodes, queryable by concrete type.
    /// </summary>
    public IReadonlyTypedSet<Node> Children =>
        this._children;

    private readonly TypedSet<Node> _children = [];
    private readonly NodesTracker<Node> _childrenTracker = new();

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        this._childrenTracker.NodeTracked += this.OnChildTracked;
        this._childrenTracker.NodeUntracked += this.OnChildUntracked;
        this._childrenTracker.Track(this);
    }

    private void OnChildTracked(Node child) =>
        this._children.Add(child);

    private void OnChildUntracked(Node child) =>
        this._children.Remove(child);

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        this._childrenTracker.Untrack();
        this._childrenTracker.NodeUntracked -= this.OnChildUntracked;
        this._childrenTracker.NodeTracked -= this.OnChildTracked;

        base._ExitTree();
    }
}
