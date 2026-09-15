using Microsoft.CodeAnalysis;

namespace ECS.Generators;

public sealed partial class HideInheritedMembersGenerator
{
    private readonly struct MemberToHide(ISymbol symbol, bool isVirtualOrOverride)
    {
        public ISymbol Symbol { get; } = symbol;
        public bool IsVirtualOrOverride { get; } = isVirtualOrOverride;
    }

    // Godot lifecycle methods owned by role generators when a role interface is present.
    private static readonly HashSet<string> _lifecycleNames = new(StringComparer.Ordinal)
    {
        "_EnterTree", "_ExitTree", "_PhysicsProcess", "_Ready", "_Notification"
    };

    private static HashSet<string> GetWhitelist(INamedTypeSymbol classSymbol, string attrFqn)
    {
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != attrFqn)
                continue;

            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var arg in attr.ConstructorArguments)
            {
                if (arg.Kind == TypedConstantKind.Array)
                {
                    foreach (var item in arg.Values)
                        if (item.Value is string itemStr) set.Add(itemStr);
                }
                else if (arg.Value is string argStr)
                {
                    set.Add(argStr);
                }
            }
            return set;
        }

        return new HashSet<string>(StringComparer.Ordinal);
    }

    private static HashSet<string> GetOwnNames(INamedTypeSymbol classSymbol) =>
        new(classSymbol.GetMembers().Select(m => m.Name), StringComparer.Ordinal);

    private static List<MemberToHide> CollectMembersToHide(
        INamedTypeSymbol classSymbol,
        HashSet<string> whitelist,
        HashSet<string> ownNames,
        bool skipLifecycle)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<MemberToHide>();

        var current = classSymbol.BaseType;
        while (current is not null
               && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in current.GetMembers())
            {
                if (member.DeclaredAccessibility != Accessibility.Public) continue;
                if (member.IsStatic) continue;

                if (member is IMethodSymbol method)
                {
                    if (method.MethodKind != MethodKind.Ordinary) continue;
                    if (method.AssociatedSymbol is IPropertySymbol or IEventSymbol) continue;
                }

                if (member is not IPropertySymbol and not IMethodSymbol and not IEventSymbol)
                    continue;

                if (IsObsolete(member)) continue;
                if (whitelist.Contains(member.Name)) continue;
                if (ownNames.Contains(member.Name)) continue;

                // When a role interface is present the role generator owns these methods.
                if (skipLifecycle && _lifecycleNames.Contains(member.Name)) continue;

                var key = BuildKey(member);
                if (!seen.Add(key)) continue;

                var isVirtual = member switch
                {
                    IMethodSymbol m => m.IsVirtual || m.IsOverride || m.IsAbstract,
                    IPropertySymbol p => p.IsVirtual || p.IsOverride || p.IsAbstract,
                    IEventSymbol e => e.IsVirtual || e.IsOverride || e.IsAbstract,
                    _ => false,
                };

                result.Add(new MemberToHide(member, isVirtual));
            }

            current = current.BaseType;
        }

        return result;
    }

    private static bool IsObsolete(ISymbol symbol) =>
        symbol.GetAttributes()
              .Any(a => a.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute");

    private static string BuildKey(ISymbol member)
    {
        if (member is IMethodSymbol m)
        {
            var pts = string.Join(",", m.Parameters.Select(p =>
                p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            var tpSuffix = m.TypeParameters.Length > 0 ? "`" + m.TypeParameters.Length : "";
            return $"M:{m.Name}{tpSuffix}({pts})";
        }

        if (member is IPropertySymbol prop)
        {
            if (prop.IsIndexer)
            {
                var pts = string.Join(",", prop.Parameters.Select(p =>
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                return $"I:({pts})";
            }
            return $"P:{prop.Name}";
        }

        return $"E:{member.Name}";
    }
}
