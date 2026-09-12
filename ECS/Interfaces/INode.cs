namespace ECS.Interfaces;

/// <summary>
/// Base interface for all ECS roles; exposes the node's <see cref="Name"/> so interfaces can be used without a concrete <see cref="Godot.Node"/> reference.
/// </summary>
public interface INode
{
    /// <summary>
    /// <para>The name of the node. This name must be unique among the siblings (other child nodes from the same parent). When set to an existing sibling's name, the node is automatically renamed.</para>
    /// <para><b>Note:</b> When changing the name, the following characters will be replaced with an underscore: (<c>.</c> <c>:</c> <c>@</c> <c>/</c> <c>"</c> <c>%</c>). In particular, the <c>@</c> character is reserved for auto-generated names. See also <c>String.validate_node_name</c>.</para>
    /// </summary>
    string Name { get; }
}
