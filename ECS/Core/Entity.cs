using ECS.Interfaces;
using Godot;

namespace ECS.Core;

/// <summary>
/// Base class for all game entities; extends <see cref="Godot.CharacterBody3D"/> and implements <see cref="IEntity"/>.
/// </summary>
[GlobalClass]
[HideInheritedMembers("Name")]
// TODO: Right now based on how EntityGenerator is built, Entity requires to be Node3D.
// I would like to spend some time investigating if it could be converted into a Node.
public partial class Entity : Node3D, IEntity
{
    string INode.Name => base.Name;
}
