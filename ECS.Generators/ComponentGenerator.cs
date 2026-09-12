using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ECS.Generators;

/// <summary>
/// For every class implementing <c>ECS.Interfaces.IComponent</c>, generates the full component
/// boilerplate: entity reference, sibling tracker, lifecycle overrides, and hooks.
/// </summary>
[Generator]
public sealed class ComponentGenerator : IIncrementalGenerator
{
    private const string TargetInterface = "ECS.Interfaces.IComponent";

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
        if (!own.Contains("Entity"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// The entity that owns this component; <c>null</c> while outside the scene tree.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected global::ECS.Interfaces.IEntity? Entity {{ get; private set; }}");
            sb.AppendLine();
        }
        if (!own.Contains("_siblings"))
            sb.AppendLine($"{indent}    private readonly global::ECS.Utils.TypedSet<global::ECS.Interfaces.IComponent> _siblings = [];");
        if (!own.Contains("_siblingsTracker"))
            sb.AppendLine($"{indent}    private readonly global::ECS.Utils.NodesTracker<global::ECS.Interfaces.IComponent> _siblingsTracker = new() {{ DirectChildren = true }};");

        if (!own.Contains("_siblings") || !own.Contains("_siblingsTracker"))
            sb.AppendLine();

        // --- Siblings property ---
        if (!own.Contains("Siblings"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Live typed set of sibling <see cref=\"global::ECS.Interfaces.IComponent\"/> nodes attached to the same entity.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected global::ECS.Utils.IReadonlyTypedSet<global::ECS.Interfaces.IComponent> Siblings => this._siblings;");
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
            if (!own.Contains("Entity"))
                sb.AppendLine($"{indent}        this.Entity = this.FindEntity();");
            sb.AppendLine($"{indent}        this._siblingsTracker.NodeTracked += this.OnSiblingTrackedInternal;");
            sb.AppendLine($"{indent}        this._siblingsTracker.NodeUntracked += this.OnSiblingUntrackedInternal;");
            sb.AppendLine($"{indent}        this._siblingsTracker.Track((global::Godot.Node)this.Entity);");
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
            sb.AppendLine($"{indent}        this._siblingsTracker.Untrack();");
            sb.AppendLine($"{indent}        this._siblingsTracker.NodeUntracked -= this.OnSiblingUntrackedInternal;");
            sb.AppendLine($"{indent}        this._siblingsTracker.NodeTracked -= this.OnSiblingTrackedInternal;");
            if (!own.Contains("Entity"))
                sb.AppendLine($"{indent}        this.Entity = null;");
            sb.AppendLine($"{indent}        base._ExitTree();");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        // --- Private tracker callbacks ---
        if (!own.Contains("OnSiblingTrackedInternal"))
        {
            sb.AppendLine($"{indent}    private void OnSiblingTrackedInternal(global::ECS.Interfaces.IComponent component)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        this._siblings.Add(component);");
            sb.AppendLine($"{indent}        this.OnSiblingTracked(component);");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnSiblingUntrackedInternal"))
        {
            sb.AppendLine($"{indent}    private void OnSiblingUntrackedInternal(global::ECS.Interfaces.IComponent component)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        this._siblings.Remove(component);");
            sb.AppendLine($"{indent}        this.OnSiblingUntracked(component);");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }

        if (!own.Contains("FindEntity"))
        {
            sb.AppendLine($"{indent}    private global::ECS.Interfaces.IEntity FindEntity()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        global::Godot.Node? instance = this;");
            sb.AppendLine($"{indent}        do");
            sb.AppendLine($"{indent}        {{");
            sb.AppendLine($"{indent}            var owner = instance.GetOwnerOrNull<global::ECS.Interfaces.IEntity>();");
            sb.AppendLine($"{indent}            if (owner != null)");
            sb.AppendLine($"{indent}                return owner;");
            sb.AppendLine();
            sb.AppendLine($"{indent}            instance = instance.GetParent();");
            sb.AppendLine($"{indent}        }} while (instance != null);");
            sb.AppendLine();
            sb.AppendLine($"{indent}        throw new InvalidOperationException($\"The component {{this.Name}} should contain an owner whose implements the interface {{typeof(global::ECS.Interfaces.IEntity).Name}}\");");
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
        if (!own.Contains("OnUpdate"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called every physics frame while the component is in the scene tree.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnUpdate(double delta) {{ }}");
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
        if (!own.Contains("OnSiblingTracked"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called when a sibling <see cref=\"global::ECS.Interfaces.IComponent\"/> is added to the owning entity.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnSiblingTracked(global::ECS.Interfaces.IComponent component) {{ }}");
            sb.AppendLine();
        }
        if (!own.Contains("OnSiblingUntracked"))
        {
            sb.AppendLine($"{indent}    /// <summary>");
            sb.AppendLine($"{indent}    /// Called when a sibling <see cref=\"global::ECS.Interfaces.IComponent\"/> is removed from the owning entity.");
            sb.AppendLine($"{indent}    /// </summary>");
            sb.AppendLine($"{indent}    protected virtual void OnSiblingUntracked(global::ECS.Interfaces.IComponent component) {{ }}");
            sb.AppendLine();
        }

        RoleGeneratorHelper.WriteFooter(sb, ns);
        spc.AddSource(RoleGeneratorHelper.HintName(classSymbol, "Component.g.cs"),
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool ImplementsDirectly(INamedTypeSymbol classSymbol, string ifaceFqn)
    {
        foreach (var iface in classSymbol.Interfaces)
            if (iface.ToDisplayString() == ifaceFqn) return true;
        return false;
    }
}
