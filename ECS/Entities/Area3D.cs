using ECS.Interfaces;
using Godot;

namespace ECS.Entities;

/// <summary>
/// ECS-aware wrapper around <see cref="Godot.Area3D"/> that implements <see cref="IEntity"/>.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name", "SceneFilePath")]
public partial class Area3D : Godot.Area3D, IEntity
{
}
