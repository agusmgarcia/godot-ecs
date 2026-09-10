using ECS.Core;
using Godot;

namespace ECS.Components;

/// <summary>
/// Component that stores the entity's height in metres as an inspector-editable value.
/// </summary>
[GlobalClass]
public partial class Height : Component<float>
{
    /// <summary>
    /// The entity's height in metres.
    /// </summary>
    [Export(PropertyHint.Range, "0,100,or_greater,hide_control,suffix:m")]
    public new float Value
    {
        get => base.Value;
        protected set => base.Value = value;
    }

    /// <summary>
    /// Initialises the component with a height of zero.
    /// </summary>
    public Height()
        : base(0) { }
}
