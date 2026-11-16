using ECS.Components;

namespace ECS.Interfaces;

/// <summary>
/// // TODO: document this.
/// </summary>
public interface IDialogable
{
    /// <summary>
    /// <para>The name of the node. This name must be unique among the siblings (other child nodes from the same parent). When set to an existing sibling's name, the node is automatically renamed.</para>
    /// <para><b>Note:</b> When changing the name, the following characters will be replaced with an underscore: (<c>.</c> <c>:</c> <c>@</c> <c>/</c> <c>"</c> <c>%</c>). In particular, the <c>@</c> character is reserved for auto-generated names. See also <c>String.validate_node_name</c>.</para>
    /// </summary>
    string Name { get; }

    /// <summary>
    /// <para>The original scene's file path, if the node has been instantiated from a <see cref="Godot.PackedScene"/> file. Only scene root nodes contains this.</para>
    /// </summary>
    string SceneFilePath { get; }

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    bool IsTalking();

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    void Talk(Dialogue dialogue);

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    bool IsListening();

    /// <summary>
    /// // TODO: document this.
    /// </summary>
    void Listen(Dialogue dialogue);
}