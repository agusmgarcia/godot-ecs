using ECS.Interfaces;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all systems; extends <see cref="Godot.Node"/> and implements <see cref="ISystem"/>.
/// </summary>
[HideInheritedMembers("Name")]
public abstract partial class System : Node, ISystem
{
}
