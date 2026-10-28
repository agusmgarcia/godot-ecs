using Godot;

namespace ECS.Components;

/// <summary>
/// Component that stores the entity's height in metres as an inspector-editable value.
/// </summary>
[GlobalClass]
public sealed partial class Height : Node
{
    /// <summary>
    /// The entity's height in metres.
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:m")]
    public float Value { get; private set; }
}
