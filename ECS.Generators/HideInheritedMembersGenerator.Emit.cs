using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ECS.Generators;

public sealed partial class HideInheritedMembersGenerator
{
    private static readonly SymbolDisplayFormat _fqf =
        SymbolDisplayFormat.FullyQualifiedFormat;

    private static void EmitMember(StringBuilder sb, MemberToHide mth)
    {
        sb.AppendLine("        /// <inheritdoc/>");
        sb.AppendLine("        [EditorBrowsable(EditorBrowsableState.Never)]");

        switch (mth.Symbol)
        {
            case IPropertySymbol prop when prop.IsIndexer:
                EmitIndexer(sb, prop, mth.IsVirtualOrOverride); break;
            case IPropertySymbol prop:
                EmitProperty(sb, prop, mth.IsVirtualOrOverride); break;
            case IMethodSymbol method:
                EmitMethod(sb, method, mth.IsVirtualOrOverride); break;
            case IEventSymbol evt:
                EmitEvent(sb, evt, mth.IsVirtualOrOverride); break;
        }

        sb.AppendLine();
    }

    private static void EmitProperty(StringBuilder sb, IPropertySymbol prop, bool isVirtual)
    {
        var type    = prop.Type.ToDisplayString(_fqf);
        var keyword = isVirtual ? "sealed override" : "new";
        sb.AppendLine($"        public {keyword} {type} {prop.Name}");
        sb.AppendLine("        {");
        if (!prop.IsWriteOnly)
            sb.AppendLine($"            get => base.{prop.Name};");
        if (prop.SetMethod is not null)
            sb.AppendLine($"            set => base.{prop.Name} = value;");
        sb.AppendLine("        }");
    }

    private static void EmitIndexer(StringBuilder sb, IPropertySymbol prop, bool isVirtual)
    {
        var type      = prop.Type.ToDisplayString(_fqf);
        var paramList = BuildParamList(prop.Parameters);
        var argList   = BuildArgList(prop.Parameters);
        var keyword   = isVirtual ? "sealed override" : "new";
        sb.AppendLine($"        public {keyword} {type} this[{paramList}]");
        sb.AppendLine("        {");
        if (!prop.IsWriteOnly)
            sb.AppendLine($"            get => base[{argList}];");
        if (prop.SetMethod is not null)
            sb.AppendLine($"            set => base[{argList}] = value;");
        sb.AppendLine("        }");
    }

    private static void EmitMethod(StringBuilder sb, IMethodSymbol method, bool isVirtual)
    {
        var ret         = method.ReturnType.ToDisplayString(_fqf);
        var tpDecl      = method.TypeParameters.Length > 0
            ? "<" + string.Join(", ", method.TypeParameters.Select(tp => tp.Name)) + ">"
            : string.Empty;
        var paramList   = BuildParamList(method.Parameters);
        var argList     = BuildArgList(method.Parameters);
        var constraints = BuildConstraints(method.TypeParameters);
        var keyword     = isVirtual ? "sealed override" : "new";
        var sig         = $"        public {keyword} {ret} {method.Name}{tpDecl}({paramList}){constraints}";

        if (method.ReturnsVoid)
        {
            sb.AppendLine(sig);
            sb.AppendLine("        {");
            sb.AppendLine($"            base.{method.Name}{tpDecl}({argList});");
            sb.AppendLine("        }");
        }
        else
        {
            sb.AppendLine($"{sig} =>");
            sb.AppendLine($"            base.{method.Name}{tpDecl}({argList});");
        }
    }

    private static void EmitEvent(StringBuilder sb, IEventSymbol evt, bool isVirtual)
    {
        var type    = evt.Type.ToDisplayString(_fqf);
        var keyword = isVirtual ? "sealed override" : "new";
        sb.AppendLine($"        public {keyword} event {type} {evt.Name}");
        sb.AppendLine("        {");
        sb.AppendLine($"            add    => base.{evt.Name} += value;");
        sb.AppendLine($"            remove => base.{evt.Name} -= value;");
        sb.AppendLine("        }");
    }

    // C# reserved keywords that Godot uses as parameter names.
    private static readonly HashSet<string> _csharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
        "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
        "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
        "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    };

    private static string EscapeName(string name) =>
        _csharpKeywords.Contains(name) ? "@" + name : name;

    private static string BuildParamList(ImmutableArray<IParameterSymbol> parameters)
    {
        if (parameters.IsEmpty) return string.Empty;
        return string.Join(", ", parameters.Select(p =>
        {
            var type     = p.Type.ToDisplayString(_fqf);
            var modifier = p.RefKind switch
            {
                RefKind.Ref => "ref ", RefKind.Out => "out ", RefKind.In => "in ", _ => ""
            };
            var paramsKw = p.IsParams ? "params " : "";
            var defVal   = p.HasExplicitDefaultValue ? " = " + RenderDefault(p) : "";
            return $"{paramsKw}{modifier}{type} {EscapeName(p.Name)}{defVal}";
        }));
    }

    private static string BuildArgList(ImmutableArray<IParameterSymbol> parameters)
    {
        if (parameters.IsEmpty) return string.Empty;
        return string.Join(", ", parameters.Select(p =>
        {
            var modifier = p.RefKind switch
            {
                RefKind.Ref => "ref ", RefKind.Out => "out ", RefKind.In => "in ", _ => ""
            };
            return $"{modifier}{EscapeName(p.Name)}";
        }));
    }

    private static string BuildConstraints(ImmutableArray<ITypeParameterSymbol> typeParams)
    {
        if (typeParams.IsEmpty) return string.Empty;
        var parts = new List<string>();
        foreach (var tp in typeParams)
        {
            var cs = new List<string>();
            if (tp.HasReferenceTypeConstraint)  cs.Add("class");
            if (tp.HasValueTypeConstraint)      cs.Add("struct");
            if (tp.HasUnmanagedTypeConstraint)  cs.Add("unmanaged");
            if (tp.HasNotNullConstraint)        cs.Add("notnull");
            foreach (var ct in tp.ConstraintTypes)
                cs.Add(ct.ToDisplayString(_fqf));
            if (tp.HasConstructorConstraint)    cs.Add("new()");
            if (cs.Count > 0)
                parts.Add($" where {tp.Name} : {string.Join(", ", cs)}");
        }
        return string.Concat(parts);
    }

    private static string RenderDefault(IParameterSymbol p)
    {
        if (p.ExplicitDefaultValue is null)
            return p.Type.IsReferenceType ? "default!" : "default";

        if (p.ExplicitDefaultValue is bool b)   return b ? "true" : "false";
        if (p.ExplicitDefaultValue is string s) return "\"" + s + "\"";
        if (p.ExplicitDefaultValue is char c)   return "'" + c + "'";

        if (p.ExplicitDefaultValue is float fv)
            return fv.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f";
        if (p.ExplicitDefaultValue is double dv)
            return dv.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "d";
        if (p.ExplicitDefaultValue is long lv)
            return lv.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";
        if (p.ExplicitDefaultValue is uint uv)
            return uv.ToString(System.Globalization.CultureInfo.InvariantCulture) + "U";
        if (p.ExplicitDefaultValue is ulong ulv)
            return ulv.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL";

        return p.ExplicitDefaultValue is IFormattable fmt
            ? fmt.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
            : p.ExplicitDefaultValue.ToString() ?? "default";
    }
}
