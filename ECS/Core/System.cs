using ECS.Utils;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all systems; automatically tracks every <see cref="Entity"/> in the scene tree.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name")]
public partial class System : Node
{
    /// <summary>
    /// Live set of all entities currently present in the scene tree.
    /// </summary>
    protected IReadOnlySet<Entity> Entities =>
        this._entitiesTracker.Nodes;

    private readonly NodesTracker<Entity> _entitiesTracker = new();

    /// <summary>
    /// Called once after the system enters the scene tree and internal state is ready.
    /// </summary>
    protected virtual void OnInit()
    {
        this._entitiesTracker.NodeTracked += this.OnEntityTracked;
        this._entitiesTracker.NodeUntracked += this.OnEntityUntracked;
        this._entitiesTracker.Track(base.GetTree().Root);
    }

    /// <summary>
    /// Called when an entity enters the scene tree.
    /// </summary>
    protected virtual void OnEntityTracked(Entity entity) { }

    /// <summary>
    /// Called when an entity exits the scene tree.
    /// </summary>
    protected virtual void OnEntityUntracked(Entity entity) { }

    /// <summary>
    /// Called once before the system exits the scene tree and internal state is torn down.
    /// </summary>
    protected virtual void OnDispose()
    {
        this._entitiesTracker.Untrack();
        this._entitiesTracker.NodeUntracked -= this.OnEntityUntracked;
        this._entitiesTracker.NodeTracked -= this.OnEntityTracked;
    }
}
