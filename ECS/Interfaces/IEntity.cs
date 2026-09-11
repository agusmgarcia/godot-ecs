using ECS.Utils;

namespace ECS.Interfaces;

/// <summary>
/// Marker interface for all game entities; exposes the entity's live component set, direct child entities, and parent entity.
/// </summary>
public interface IEntity : INode
{
    /// <summary>
    /// Live typed set of all <see cref="IComponent"/> children attached directly to this entity.
    /// </summary>
    IReadonlyTypedSet<IComponent> Components { get; }

    /// <summary>
    /// Live set of all direct child entities of this entity.
    /// </summary>
    IReadOnlySet<IEntity> Children { get; }

    /// <summary>
    /// The parent entity of this entity, or <c>null</c> if there is none.
    /// </summary>
    IEntity? Parent { get; }
}
