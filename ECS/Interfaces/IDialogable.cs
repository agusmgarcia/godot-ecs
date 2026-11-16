using ECS.Components;
using Godot;

namespace ECS.Interfaces;

/// <summary>
/// Contract for nodes that can participate in a Dialogue session.
/// </summary>
public interface IDialogable
{
    /// <summary>
    /// <para>The name of the node. This name must be unique among the siblings (other child nodes from the same parent). When set to an existing sibling's name, the node is automatically renamed.</para>
    /// <para><b>Note:</b> When changing the name, the following characters will be replaced with an underscore: (<c>.</c> <c>:</c> <c>@</c> <c>/</c> <c>"</c> <c>%</c>). In particular, the <c>@</c> character is reserved for auto-generated names. See also <c>String.validate_node_name</c>.</para>
    /// </summary>
    StringName Name { get; }

    /// <summary>
    /// <para>The original scene's file path, if the node has been instantiated from a <see cref="Godot.PackedScene"/> file. Only scene root nodes contains this.</para>
    /// </summary>
    string SceneFilePath { get; }

    /// <summary>
    /// True if this node is player-controlled and requires the dialogue UI.
    /// </summary>
    bool Main { get; }

    /// <summary>
    /// Optional portrait texture shown in the dialogue balloon UI.
    /// </summary>
    Texture2D? Portrait { get; }

    /// <summary>
    /// Transitions this node back to its idle/default animation state.
    /// </summary>
    void Idle();

    /// <summary>
    /// Transitions this node into its talking animation for the dialogue.
    /// </summary>
    void Talk(Dialogue dialogue);

    /// <summary>
    /// Transitions this node into its listening animation for the dialogue.
    /// </summary>
    void Listen(Dialogue dialogue);
}