using ECS.Utils;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all systems; automatically tracks every <see cref="Entity"/> in the scene tree.
/// </summary>
[GlobalClass]
public partial class System : Node
{
    /// <summary>
    /// Live set of all entities currently present in the scene tree.
    /// </summary>
    protected IReadOnlySet<Entity> Entities =>
        this._entitiesTracker.Nodes;

    private readonly NodesTracker<Entity> _entitiesTracker = new();

    public override void _EnterTree()
    {
        base._EnterTree();

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

    public override void _ExitTree()
    {
        this._entitiesTracker.Untrack();
        this._entitiesTracker.NodeUntracked -= this.OnEntityUntracked;
        this._entitiesTracker.NodeTracked -= this.OnEntityTracked;

        base._ExitTree();
    }
}