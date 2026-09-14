using ECS.Interfaces;
using Godot;

namespace ECS.Entities;

/// <summary>
/// ECS-aware wrapper around <see cref="Godot.CollisionShape3D"/> that implements <see cref="IEntity"/>.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name", "SceneFilePath", "Shape")]
public partial class CollisionShape3D : Godot.CollisionShape3D, IEntity
{
}
