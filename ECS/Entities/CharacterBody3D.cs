using ECS.Interfaces;
using Godot;

namespace ECS.Entities;

/// <summary>
/// Base class for all game entities; extends <see cref="Godot.CharacterBody3D"/> and implements <see cref="IEntity"/>.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name")]
public partial class CharacterBody3D : Godot.CharacterBody3D, IEntity
{
    string INode.Name => base.Name;
}
