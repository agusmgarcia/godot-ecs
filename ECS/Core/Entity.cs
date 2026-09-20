using ECS.Interfaces;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all game entities; extends <see cref="Godot.CharacterBody3D"/> and implements <see cref="IEntity"/>.
/// </summary>
[HideInheritedMembers("Name", "Position", "Rotation", "Scale", "SceneFilePath")]
public abstract partial class Entity : Node3D, IEntity
{
}
