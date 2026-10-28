using ECS.Utils;
using Godot;

namespace ECS.Components;

/// <summary>
/// CollisionShape3D that scales its capsule/cylinder to match a sibling Height.
/// </summary>
[GlobalClass]
public sealed partial class AutoTransformCollisionShape3D : CollisionShape3D
{
    private Height? _height;

    private float _initialHeight;
    private float _initialRadius;

    /// <summary>
    /// Registers the sibling Height tracker to auto-scale this shape.
    /// </summary>
    public AutoTransformCollisionShape3D()
    {
        this.TrackSiblings<Height>(
            this.OnHeightTracked,
            this.OnHeightUntracked,
            unique: true);
    }

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        base._EnterTree();

        if (base.Shape is CapsuleShape3D capsule)
        {
            this._initialHeight = capsule.Height;
            this._initialRadius = capsule.Radius;
        }
        else if (base.Shape is CylinderShape3D cylinder)
        {
            this._initialHeight = cylinder.Height;
            this._initialRadius = cylinder.Radius;
        }
        else
        {
            throw new NotImplementedException($"Shape of type '{base.Shape.GetType().Name}' is not supported");
        }
    }

    private void OnHeightTracked(Height height)
    {
        this._height = height;

        if (base.Shape is CapsuleShape3D capsule)
        {
            capsule.Height = this._height.Value;
            capsule.Radius = this._initialRadius * (this._height.Value / this._initialHeight);
        }
        else if (base.Shape is CylinderShape3D cylinder)
        {
            cylinder.Height = this._height.Value;
            cylinder.Radius = this._initialRadius * (this._height.Value / this._initialHeight);
        }
        else
        {
            throw new NotImplementedException($"Shape of type '{base.Shape.GetType().Name}' is not supported");
        }
    }

    private void OnHeightUntracked(Height height)
    {
        if (base.Shape is CapsuleShape3D capsule)
        {
            capsule.Radius = this._initialRadius;
            capsule.Height = this._initialHeight;
        }
        else if (base.Shape is CylinderShape3D cylinder)
        {
            cylinder.Radius = this._initialRadius;
            cylinder.Height = this._initialHeight;
        }
        else
        {
            throw new NotImplementedException($"Shape of type '{base.Shape.GetType().Name}' is not supported");
        }

        this._height = null;
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        if (base.Shape is CapsuleShape3D capsule)
        {
            this._initialHeight = capsule.Height;
            this._initialRadius = capsule.Radius;
        }
        else if (base.Shape is CylinderShape3D cylinder)
        {
            this._initialHeight = cylinder.Height;
            this._initialRadius = cylinder.Radius;
        }
        else
        {
            throw new NotImplementedException($"Shape of type '{base.Shape.GetType().Name}' is not supported");
        }

        base._ExitTree();
    }
}
