using ECS.Interfaces;

namespace ECS.Entities;

/// <summary>
/// ECS-aware wrapper around <see cref="Godot.CollisionShape3D"/> that implements <see cref="IEntity"/>.
/// </summary>
[HideInheritedMembers("Name", "Position", "Rotation", "Scale", "SceneFilePath", "Shape")]
public abstract partial class CollisionShape3D : Godot.CollisionShape3D, IEntity
{
}
