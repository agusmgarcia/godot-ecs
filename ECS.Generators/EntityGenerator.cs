using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ECS.Generators;

/// <summary>
/// For every class implementing <c>ECS.Interfaces.IEntity</c>, generates the full entity
/// boilerplate: component tracker, child-entity tracker, the three Godot lifecycle
/// overrides, and the <c>Components</c>, <c>Children</c>, and <c>Parent</c> members.
/// </summary>
[Generator]
public sealed class EntityGenerator : IIncrementalGenerator
{
    private const string TargetInterface = "ECS.Interfaces.IEntity";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax c && c.BaseList != null,
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(static c => c is not null);

        var combined = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(combined, static (spc, source) =>
        {
            var (compilation, classes) = source;
            foreach (var classDecl in classes)
                Generate(spc, compilation, classDecl);
        });
    }

    private static void Generate(
        SourceProductionContext spc,
        Compilation compilation,
        ClassDeclarationSyntax classDecl)
    {
        var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
        if (model.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol) return;

        // Only act when the class directly implements IEntity (not inherited).
        if (!ImplementsDirectly(classSymbol, TargetInterface)) return;

        var own = RoleGeneratorHelper.GetOwnNames(classSymbol);

        var sb = new StringBuilder();
        RoleGeneratorHelper.WriteHeader(sb, classSymbol, out var ns, out var indent);

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
            sb.AppendLine($"{indent}        base.SetNotifyLocalTransform(true);");
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

        // --- _Notification ---
        if (!own.Contains("_Notification"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine($"{indent}    public sealed override void _Notification(int what)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        if (what == global::Godot.Node3D.NotificationLocalTransformChanged)");
            sb.AppendLine($"{indent}        {{");
            sb.AppendLine($"{indent}            foreach (var position in this.Components.GetAll<global::ECS.Components.Position>())");
            sb.AppendLine($"{indent}            {{");
            sb.AppendLine($"{indent}                position.NotificationFromParent = true;");
            sb.AppendLine($"{indent}                position.Value = base.Position;");
            sb.AppendLine($"{indent}                position.NotificationFromParent = false;");
            sb.AppendLine($"{indent}            }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}            foreach (var rotation in this.Components.GetAll<global::ECS.Components.Rotation>())");
            sb.AppendLine($"{indent}            {{");
            sb.AppendLine($"{indent}                rotation.NotificationFromParent = true;");
            sb.AppendLine($"{indent}                rotation.Value = base.Rotation;");
            sb.AppendLine($"{indent}                rotation.NotificationFromParent = false;");
            sb.AppendLine($"{indent}            }}");
            sb.AppendLine($"{indent}        }}");
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
            sb.AppendLine($"{indent}        this._childrenTracker.Untrack();");
            sb.AppendLine($"{indent}        this._childrenTracker.NodeUntracked -= this.OnChildUntracked;");
            sb.AppendLine($"{indent}        this._childrenTracker.NodeTracked -= this.OnChildTracked;");
            sb.AppendLine($"{indent}        this._componentsTracker.Untrack();");
            sb.AppendLine($"{indent}        this._componentsTracker.NodeUntracked -= this.OnComponentUntracked;");
            sb.AppendLine($"{indent}        this._componentsTracker.NodeTracked -= this.OnComponentTracked;");
            sb.AppendLine($"{indent}        base.SetNotifyLocalTransform(false);");
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

        RoleGeneratorHelper.WriteFooter(sb, ns);

        var hint = RoleGeneratorHelper.HintName(classSymbol, "Entity.g.cs");
        spc.AddSource(hint, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool ImplementsDirectly(INamedTypeSymbol classSymbol, string ifaceFqn)
    {
        foreach (var iface in classSymbol.Interfaces)
            if (iface.ToDisplayString() == ifaceFqn) return true;
        return false;
    }
}
