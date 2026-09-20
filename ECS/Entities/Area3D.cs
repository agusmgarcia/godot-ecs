using ECS.Interfaces;

namespace ECS.Entities;

/// <summary>
/// ECS-aware wrapper around <see cref="Godot.Area3D"/> that implements <see cref="IEntity"/>.
/// </summary>
[HideInheritedMembers("Name", "Position", "Rotation", "Scale", "SceneFilePath")]
public abstract partial class Area3D : Godot.Area3D, IEntity
{
}
