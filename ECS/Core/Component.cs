using ECS.Interfaces;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all components; extends <see cref="Godot.Node"/> and implements <see cref="IComponent"/>.
/// </summary>
[HideInheritedMembers("Name", "Owner")]
public abstract partial class Component : Node, IComponent
{
}
