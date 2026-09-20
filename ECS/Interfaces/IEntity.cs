using ECS.Utils;
using Godot;

namespace ECS.Interfaces;

/// <summary>
/// Marker interface for all game entities; exposes the entity's live component set, direct child entities, and parent entity.
/// </summary>
public interface IEntity : INode
{
    /// <summary>
    /// Position (translation) of this node in parent space (relative to the parent node). This is equivalent to the <see cref="Godot.Node3D.Transform"/>'s <c>Transform3D.origin</c>.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// <para>Rotation of this node as <a href="https://en.wikipedia.org/wiki/Euler_angles">Euler angles</a>, in radians and in parent space (relative to the parent node). This value is obtained from <see cref="Godot.Node3D.Basis"/>'s rotation.</para>
    /// <para>- The <c>Vector3.x</c> is the angle around the local X axis (pitch);</para>
    /// <para>- The <c>Vector3.y</c> is the angle around the local Y axis (yaw);</para>
    /// <para>- The <c>Vector3.z</c> is the angle around the local Z axis (roll).</para>
    /// <para>The order of each consecutive rotation can be changed with <see cref="Godot.Node3D.RotationOrder"/> (see <see cref="Godot.EulerOrder"/> constants). In Godot, Euler angles always use intrinsic order. By default, the intrinsic YXZ convention is used (<see cref="Godot.EulerOrder.Yxz"/>).</para>
    /// <para><b>Note:</b> This property is edited in degrees in the inspector. If you want to use degrees in a script, use <see cref="Godot.Node3D.RotationDegrees"/>.</para>
    /// </summary>
    Vector3 Rotation { get; }

    /// <summary>
    /// <para>Scale of this node in local space (relative to this node). This value is obtained from <see cref="Godot.Node3D.Basis"/>'s scale.</para>
    /// <para><b>Note:</b> The behavior of some 3D node types is not affected by this property. These include <see cref="Godot.Light3D"/>, <see cref="Godot.Camera3D"/>, <see cref="Godot.AudioStreamPlayer3D"/>, and more.</para>
    /// <para><b>Warning:</b> The scale's components must either be all positive or all negative, and <b>not</b> exactly <c>0.0</c>. Otherwise, it won't be possible to obtain the scale from the <see cref="Godot.Node3D.Basis"/>. This may cause the intended scale to be lost when reloaded from disk, and potentially other unstable behavior.</para>
    /// </summary>
    Vector3 Scale { get; }

    /// <summary>
    /// The entity's current right axis (<see cref="Godot.Basis.X"/>); <see cref="Vector3.Right"/> when outside the scene tree.
    /// </summary>
    Vector3 Right { get; }

    /// <summary>
    /// The entity's current up axis (<see cref="Godot.Basis.Y"/>); <see cref="Vector3.Up"/> when outside the scene tree.
    /// </summary>
    Vector3 Up { get; }

    /// <summary>
    /// The entity's current forward axis (<see cref="Godot.Basis.Z"/>); <see cref="Vector3.Forward"/> when outside the scene tree.
    /// </summary>
    Vector3 Forward { get; }

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

    /// <summary>
    /// The original scene's file path, if the node has been instantiated from a PackedScene file. Only scene root nodes contains this.
    /// </summary>
    string SceneFilePath { get; }
}
