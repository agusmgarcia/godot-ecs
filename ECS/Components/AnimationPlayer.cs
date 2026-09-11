using ECS.Interfaces;
using Godot;

namespace ECS.Components;

/// <summary>
/// ECS-aware wrapper around <see cref="Godot.AnimationPlayer"/> that implements <see cref="IComponent"/>.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name")]
public partial class AnimationPlayer : Godot.AnimationPlayer, IComponent
{
    string INode.Name => base.Name;
}
