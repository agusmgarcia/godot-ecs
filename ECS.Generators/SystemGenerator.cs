using System.Text;
using Microsoft.CodeAnalysis;

namespace ECS.Generators;

/// <summary>
/// For every class implementing <c>ECS.Interfaces.ISystem</c>, generates the full system
/// boilerplate: entity tracker, lifecycle overrides, and hooks.
/// </summary>
[Generator]
public sealed class SystemGenerator : RoleGenerator
{
    /// <inheritdoc/>
    protected override string TargetInterface => "ECS.Interfaces.ISystem";

    /// <inheritdoc/>
    protected override string HintSuffix => "System.g.cs";

    /// <inheritdoc/>
    protected override void EmitMembers(
        INamedTypeSymbol classSymbol,
        HashSet<string> own,
        StringBuilder sb,
        string indent)
    {
        // --- Fields & properties ---
        if (!own.Contains("_entities"))
            sb.AppendLine($"{indent}    private readonly global::ECS.Utils.TypedSet<global::ECS.Interfaces.IEntity> _entities = [];");
        if (!own.Contains("_entitiesTracker"))
            sb.AppendLine($"{indent}    private readonly global::ECS.Utils.NodesTracker<global::ECS.Interfaces.IEntity> _entitiesTracker = new();");
        sb.AppendLine();

        if (!own.Contains("Entities"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Live typed set of all <see cref=\"global::ECS.Interfaces.IEntity\"/> instances currently in the scene tree.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected global::ECS.Utils.IReadonlyTypedSet<global::ECS.Interfaces.IEntity> Entities => this._entities;");
            sb.AppendLine();
        }

        // --- _EnterTree ---
        if (!own.Contains("_EnterTree"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"{indent}    public sealed override void _EnterTree()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        base._EnterTree();");
            sb.AppendLine($"{indent}        this._entitiesTracker.NodeTracked += this.OnEntityTrackedInternal;");
            sb.AppendLine($"{indent}        this._entitiesTracker.NodeUntracked += this.OnEntityUntrackedInternal;");
            sb.AppendLine($"{indent}        this._entitiesTracker.Track(base.GetTree().Root);");
            sb.AppendLine($"{indent}        this.OnInit();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        // --- _Ready ---
        if (!own.Contains("_Ready"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"{indent}    public sealed override void _Ready() =>");
            sb.AppendLine($"{indent}        base._Ready();");
            sb.AppendLine();
        }

        // --- _PhysicsProcess ---
        if (!own.Contains("_PhysicsProcess"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"{indent}    public sealed override void _PhysicsProcess(double delta)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        base._PhysicsProcess(delta);");
            sb.AppendLine($"{indent}        this.OnUpdate(delta);");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        // --- _ExitTree ---
        if (!own.Contains("_ExitTree"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"{indent}    public sealed override void _ExitTree()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        this.OnDispose();");
            sb.AppendLine($"{indent}        this._entitiesTracker.Untrack();");
            sb.AppendLine($"{indent}        this._entitiesTracker.NodeUntracked -= this.OnEntityUntrackedInternal;");
            sb.AppendLine($"{indent}        this._entitiesTracker.NodeTracked -= this.OnEntityTrackedInternal;");
            sb.AppendLine($"{indent}        base._ExitTree();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        // --- Private tracker callbacks ---
        if (!own.Contains("OnEntityTrackedInternal"))
        {
            sb.AppendLine($"{indent}    private void OnEntityTrackedInternal(global::ECS.Interfaces.IEntity entity)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        this._entities.Add(entity);");
            sb.AppendLine($"{indent}        this.OnEntityTracked(entity);");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnEntityUntrackedInternal"))
        {
            sb.AppendLine($"{indent}    private void OnEntityUntrackedInternal(global::ECS.Interfaces.IEntity entity)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        this._entities.Remove(entity);");
            sb.AppendLine($"{indent}        this.OnEntityUntracked(entity);");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        // --- Virtual hooks ---
        if (!own.Contains("OnInit"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called once after the system enters the scene tree.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnInit() {{ }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnUpdate"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called every physics frame while the system is in the scene tree.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnUpdate(double delta) {{ }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnDispose"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called once before the system exits the scene tree.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnDispose() {{ }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnEntityTracked"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called when an <see cref=\"global::ECS.Interfaces.IEntity\"/> enters the scene tree.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnEntityTracked(global::ECS.Interfaces.IEntity entity) {{ }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnEntityUntracked"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called when an <see cref=\"global::ECS.Interfaces.IEntity\"/> exits the scene tree.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnEntityUntracked(global::ECS.Interfaces.IEntity entity) {{ }}");
            sb.AppendLine();
        }
    }
}
