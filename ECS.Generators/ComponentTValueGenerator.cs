using System.Text;
using Microsoft.CodeAnalysis;

namespace ECS.Generators;

/// <summary>
/// For every class directly implementing <c>ECS.Interfaces.IComponent{TValue}</c>, generates the
/// full component boilerplate (by delegating to <see cref="ComponentGenerator"/>) plus the
/// <c>Value</c> property and <c>ValueChanged</c> event.
/// </summary>
[Generator]
public sealed class ComponentTValueGenerator : ComponentGenerator
{
    private const string ValueInterfaceOriginalDef = "ECS.Interfaces.IComponent<TValue>";

    /// <inheritdoc/>
    protected override string TargetInterface => "ECS.Interfaces.IComponent<TValue>";

    /// <inheritdoc/>
    protected override string HintSuffix => "Component.g.cs";

    /// <inheritdoc/>
    protected override bool ShouldProcess(INamedTypeSymbol classSymbol)
    {
        foreach (var iface in classSymbol.Interfaces)
            if (iface.IsGenericType &&
                iface.OriginalDefinition.ToDisplayString() == ValueInterfaceOriginalDef)
                return true;
        return false;
    }

    /// <inheritdoc/>
    protected override void EmitMembers(
        INamedTypeSymbol classSymbol,
        HashSet<string> own,
        StringBuilder sb,
        string indent)
    {
        // Emit standard IComponent boilerplate (skipped automatically when base already has it).
        base.EmitMembers(classSymbol, own, sb, indent);

        // Resolve TValue from the directly-listed IComponent<TValue> interface.
        INamedTypeSymbol? valueIface = null;
        foreach (var iface in classSymbol.Interfaces)
            if (iface.IsGenericType &&
                iface.OriginalDefinition.ToDisplayString() == ValueInterfaceOriginalDef)
            {
                valueIface = iface;
                break;
            }

        if (valueIface == null) return;

        var tValue = valueIface.TypeArguments[0].ToDisplayString(
            global::Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat
                .WithMiscellaneousOptions(
                    global::Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                    global::Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions.UseSpecialTypes));

        // --- ValueChanged event ---
        if (!own.Contains("ValueChanged"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    public event global::System.Action<{tValue}>? ValueChanged;");
            sb.AppendLine();
        }

        // --- Value property ---
        if (!own.Contains("Value"))
        {
            sb.AppendLine($"{indent}    /// <inheritdoc/>");
            sb.AppendLine($"{indent}    public {tValue} Value");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        get;");
            sb.AppendLine($"{indent}        protected set");
            sb.AppendLine($"{indent}        {{");
            sb.AppendLine($"{indent}            if (global::System.Collections.Generic.EqualityComparer<{tValue}>.Default.Equals(field, value))");
            sb.AppendLine($"{indent}                return;");
            sb.AppendLine();
            sb.AppendLine($"{indent}            field = value;");
            sb.AppendLine($"{indent}            this.ValueChanged?.Invoke(value);");
            sb.AppendLine($"{indent}        }}");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
        }
    }
}
