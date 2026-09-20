using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ECS.Generators;

/// <summary>
/// For every class implementing <c>ECS.Interfaces.ISystem</c>, generates the full system
/// boilerplate: entity tracker, lifecycle overrides, and hooks.
/// </summary>
[Generator]
public sealed class SystemGenerator : IIncrementalGenerator
{
    private const string TargetInterface = "ECS.Interfaces.ISystem";

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

        if (!ImplementsDirectly(classSymbol, TargetInterface)) return;

        var own = RoleGeneratorHelper.GetOwnNames(classSymbol);

        var sb = new StringBuilder();
        RoleGeneratorHelper.WriteHeader(sb, classSymbol, out var ns, out var indent);

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

        RoleGeneratorHelper.WriteFooter(sb, ns);
        spc.AddSource(RoleGeneratorHelper.HintName(classSymbol, "System.g.cs"),
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool ImplementsDirectly(INamedTypeSymbol classSymbol, string ifaceFqn)
    {
        foreach (var iface in classSymbol.Interfaces)
            if (iface.ToDisplayString() == ifaceFqn) return true;
        return false;
    }
}
