using ECS.Interfaces;
using Godot;

namespace ECS.Components;

/// <summary>
/// ECS-aware wrapper around <see cref="Godot.CollisionShape3D"/> that implements <see cref="IComponent"/>.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name")]
public partial class CollisionShape3D : Godot.CollisionShape3D, IComponent
{
}
