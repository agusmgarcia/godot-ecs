# ECS.Components

Ready-to-use `Component` implementations that cover the most common 3D game entity behaviours. All components are marked `[GlobalClass]` and are available directly from the Godot editor.

## Components

### `Main`

A marker component with no behaviour. Attach it to an entity to tag it as the "main" or primary entity of a given scene, which allows `System` subclasses to distinguish it from other entities by type.

```csharp
var main = entity.Children.GetOrNull<Main>();
```

---

### `Velocity`

Handles 3D physics velocity for a `CharacterBody3D` entity. Integrates gravity, air friction, and a configurable max speed. Automatically notifies a sibling `Rotation` component of the new look-at target whenever the velocity changes.

**Exported properties (configurable in the editor):**

| Property      | Default         | Description                                             |
| ------------- | --------------- | ------------------------------------------------------- |
| `Gravity`     | Project setting | Downward acceleration (m/s²) applied when airborne.     |
| `AirFriction` | Project setting | Horizontal speed loss per second while airborne (m/s²). |
| `MaxSpeed`    | `0`             | Maximum horizontal speed (m/s).                         |

**API:**

```csharp
// Apply an acceleration impulse in a direction this frame.
velocity.Accelerate(direction: Vector3.Forward, acceleration: 20f);

// Apply a deceleration impulse this frame.
velocity.Decelerate(deceleration: 10f);
```

Both `Accelerate` and `Decelerate` are additive within a single physics frame and are consumed (reset to zero) at the start of the next `_PhysicsProcess`.

---

### `Rotation`

Smoothly rotates the entity to face a world-space `Target` position, updating all three Euler axes (pitch, yaw, roll) each physics frame using `LerpAngle`.

**Exported properties:**

| Property       | Default | Description                           |
| -------------- | ------- | ------------------------------------- |
| `AngularSpeed` | `0`     | Rotation speed in degrees per second. |

**API:**

```csharp
// Point the entity at a world-space position.
rotation.Target = somePosition;
```

The component computes the direction vector from the entity's current `GlobalPosition` to `Target`, then derives:

- **Yaw (Y):** `Atan2(direction.X, direction.Z)`
- **Pitch (X):** `Atan2(-direction.Y, horizontalLength)`
- **Roll (Z):** always lerps to `0` (entity stays upright)

The `Velocity` component sets `Rotation.Target` automatically to face the direction of travel.

---

### `Height`

Stores the entity's height as a single `float` value (in metres). Exposes the value as an editable export in the Godot inspector.

**Exported properties:**

| Property | Default | Description       |
| -------- | ------- | ----------------- |
| `Value`  | `0`     | Height in metres. |

```csharp
float h = entity.Children.Get<Height>().Value;
```

---

### `StatesMachine<TEntity>`

A generic finite state machine component. States are pooled (`ElementsPool`) to avoid per-frame allocations. State transitions are queued and processed at the start of each `_PhysicsProcess`.

**Defining a state machine:**

```csharp
public partial class PlayerStateMachine : StatesMachine<Player>
{
    public override void _EnterTree()
    {
        base._EnterTree();
        this.SetState<IdleState, IdleState.Params>(new IdleState.Params());
    }
}
```

**Defining a state:**

```csharp
public class IdleState : StatesMachine<Player>.BaseState<IdleState.Params>
{
    public struct Params { /* transition data */ }

    protected override void OnInit()  { /* called once on entry */ }
    protected override void OnUpdate(double delta) { /* called every frame */ }
    protected override void OnDispose() { /* called once on exit */ }

    // Return false to block non-forced transitions away from this state.
    protected override bool ReadyToTransition() => true;
}
```

**Transitioning:**

```csharp
// Request a transition (blocked if the current state returns false from ReadyToTransition).
this.SetState<RunState, RunState.Params>(new RunState.Params { ... });

// Force a transition regardless of ReadyToTransition.
this.SetState<IdleState, IdleState.Params>(new IdleState.Params(), force: true);
```

If the incoming state has the same type as the current one, only `StateParams` is updated — `OnInit` and `OnDispose` are **not** called again. This is the expected behaviour when the same state is re-entered every frame with updated parameters.

**State lifecycle:**

```txt
SetState() called
    └─► queued
            └─► _PhysicsProcess: ReadyToTransition()?
                    ├─ No  → discard incoming state (returned to pool)
                    ├─ Same type → update StateParams only
                    └─ New type → OnDispose (old) → OnInit (new) → OnUpdate each frame
```
