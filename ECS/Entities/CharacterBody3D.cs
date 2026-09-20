using ECS.Interfaces;

namespace ECS.Entities;

/// <summary>
/// Base class for all game entities; extends <see cref="Godot.CharacterBody3D"/> and implements <see cref="IEntity"/>.
/// </summary>
[HideInheritedMembers("Name", "SceneFilePath")]
public abstract partial class CharacterBody3D : Godot.CharacterBody3D, IEntity
{
}
