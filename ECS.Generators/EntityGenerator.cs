using System.Text;
using Microsoft.CodeAnalysis;

namespace ECS.Generators;

/// <summary>
/// For every class implementing <c>ECS.Interfaces.IEntity</c>, generates the full entity
/// boilerplate: component tracker, child-entity tracker, the three Godot lifecycle
/// overrides, and the <c>Components</c>, <c>Children</c>, and <c>Parent</c> members.
/// </summary>
[Generator]
public sealed class EntityGenerator : RoleGenerator
{
    /// <inheritdoc/>
    protected override string TargetInterface => "ECS.Interfaces.IEntity";

    /// <inheritdoc/>
    protected override string HintSuffix => "Entity.g.cs";

    /// <inheritdoc/>
    protected override void EmitMembers(
        INamedTypeSymbol classSymbol,
        HashSet<string> own,
        StringBuilder sb,
        string indent)
    {
        // --- Fields ---
        if (!own.Contains("_components"))
            sb.AppendLine($"{indent}    private readonly global::ECS.Utils.TypedSet<global::ECS.Interfaces.IComponent> _components = [];");
        if (!own.Contains("_componentsTracker"))
            sb.AppendLine($"{indent}    private readonly global::ECS.Utils.NodesTracker<global::ECS.Interfaces.IComponent> _componentsTracker = new() {{ DirectChildren = true }};");
        if (!own.Contains("_childrenTracker"))
            sb.AppendLine($"{indent}    private readonly global::ECS.Utils.NodesTracker<global::ECS.Interfaces.IEntity> _childrenTracker = new() {{ DirectChildren = true }};");
        sb.AppendLine();

        // --- IEntity interface properties ---
        if (!own.Contains("Components"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    public global::ECS.Utils.IReadonlyTypedSet<global::ECS.Interfaces.IComponent> Components => this._components;");
            sb.AppendLine();
        }
        if (!own.Contains("Children"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    public global::System.Collections.Generic.IReadOnlySet<global::ECS.Interfaces.IEntity> Children => this._childrenTracker.Nodes;");
            sb.AppendLine();
        }
        if (!own.Contains("Parent"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    public global::ECS.Interfaces.IEntity? Parent {{ get; private set; }}");
            sb.AppendLine();
        }
        if (!own.Contains("Right"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    public global::Godot.Vector3 Right => base.Basis.X;");
            sb.AppendLine();
        }
        if (!own.Contains("Up"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    public global::Godot.Vector3 Up => base.Basis.Y;");
            sb.AppendLine();
        }
        if (!own.Contains("Forward"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    public global::Godot.Vector3 Forward => base.Basis.Z;");
            sb.AppendLine();
        }

        // --- AddComponent ---
        if (!own.Contains("AddComponent"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Adds <paramref name=\"component\"/> as a child node of this entity.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void AddComponent<TComponent>(TComponent component)");
            sb.AppendLine($"{indent}        where TComponent : global::Godot.Node, global::ECS.Interfaces.IComponent =>");
            sb.AppendLine($"{indent}          base.AddChild(component);");
            sb.AppendLine();
        }

        // --- RemoveComponent ---
        if (!own.Contains("RemoveComponent"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Removes <paramref name=\"component\"/> from this entity's children.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void RemoveComponent<TComponent>(TComponent component)");
            sb.AppendLine($"{indent}        where TComponent : global::Godot.Node, global::ECS.Interfaces.IComponent =>");
            sb.AppendLine($"{indent}          base.RemoveChild(component);");
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
            if (!own.Contains("Parent"))
                sb.AppendLine($"{indent}        this.Parent = base.GetParent<global::ECS.Interfaces.IEntity>();");
            sb.AppendLine($"{indent}        this._componentsTracker.NodeTracked += this.OnComponentTracked;");
            sb.AppendLine($"{indent}        this._componentsTracker.NodeUntracked += this.OnComponentUntracked;");
            sb.AppendLine($"{indent}        this._componentsTracker.Track(this);");
            sb.AppendLine($"{indent}        this._childrenTracker.NodeTracked += this.OnChildTracked;");
            sb.AppendLine($"{indent}        this._childrenTracker.NodeUntracked += this.OnChildUntracked;");
            sb.AppendLine($"{indent}        this._childrenTracker.Track(this);");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        // --- _Ready ---
        if (!own.Contains("_Ready"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"{indent}    public sealed override void _Ready()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        base._Ready();");
            sb.AppendLine($"{indent}        this.OnInit();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        // --- _PhysicsProcess ---
        if (!own.Contains("_PhysicsProcess"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"{indent}    public sealed override void _PhysicsProcess(double delta) =>");
            sb.AppendLine($"{indent}        base._PhysicsProcess(delta);");
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
            sb.AppendLine($"{indent}        this._childrenTracker.Untrack();");
            sb.AppendLine($"{indent}        this._childrenTracker.NodeUntracked -= this.OnChildUntracked;");
            sb.AppendLine($"{indent}        this._childrenTracker.NodeTracked -= this.OnChildTracked;");
            sb.AppendLine($"{indent}        this._componentsTracker.Untrack();");
            sb.AppendLine($"{indent}        this._componentsTracker.NodeUntracked -= this.OnComponentUntracked;");
            sb.AppendLine($"{indent}        this._componentsTracker.NodeTracked -= this.OnComponentTracked;");
            if (!own.Contains("Parent"))
                sb.AppendLine($"{indent}        this.Parent = null;");
            sb.AppendLine($"{indent}        base._ExitTree();");
            sb.AppendLine($"{indent}        base.RequestReady();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        // --- Virtual hooks ---
        if (!own.Contains("OnInit"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called once after the component enters the scene tree and internal state is ready.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnInit() {{ }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnDispose"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called once before the component exits the scene tree and internal state is torn down.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnDispose() {{ }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnComponentTracked"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called when a direct child <see cref=\"global::ECS.Interfaces.IComponent\"/> enters the scene tree; adds it to <see cref=\"Components\"/> by default.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnComponentTracked(global::ECS.Interfaces.IComponent component) =>");
            sb.AppendLine($"{indent}        this._components.Add(component);");
            sb.AppendLine();
        }
        if (!own.Contains("OnComponentUntracked"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called when a direct child <see cref=\"global::ECS.Interfaces.IComponent\"/> exits the scene tree; removes it from <see cref=\"Components\"/> by default.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnComponentUntracked(global::ECS.Interfaces.IComponent component) =>");
            sb.AppendLine($"{indent}        this._components.Remove(component);");
            sb.AppendLine();
        }

        // --- Private tracker callbacks ---
        if (!own.Contains("OnChildTracked"))
        {
            sb.AppendLine($"{indent}    private void OnChildTracked(global::ECS.Interfaces.IEntity entity) {{ }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnChildUntracked"))
        {
            sb.AppendLine($"{indent}    private void OnChildUntracked(global::ECS.Interfaces.IEntity entity) {{ }}");
            sb.AppendLine();
        }
    }
}
