using ECS.Interfaces;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all components; extends <see cref="Godot.Node"/> and implements <see cref="IComponent"/>.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name", "Owner")]
public partial class Component : Node, IComponent
{
}
