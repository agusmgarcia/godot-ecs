# godot-ecs

A lightweight Entity Component System (ECS) framework for [Godot 4](https://godotengine.org/) built in C#. A single NuGet package with three logical layers: low-level utilities, core abstractions, and ready-to-use components.

## Architecture

```txt
ECS/
├── Core/        Core ECS abstractions: Entity, Component, and System.
├── Components/  Ready-to-use components: velocity, rotation, height, state machine, animation player, area, and collision shape.
└── Utils/       Low-level utilities: object pooling, typed sets, and node tracking.
```

Each layer only depends on the layer above it: `Components` → `Core` → `Utils`.

## Getting Started

Add the package via NuGet:

```xml
<PackageReference Include="ECS" Version="0.1.0" />
```

The typical scene setup looks like this:

```txt
MyScene
├── MySystem          (extends System)
└── MyEntity          (extends Entity)
    ├── Main          (marker component)
    ├── Velocity      (physics velocity)
    ├── Rotation      (look-at rotation)
    └── MyStateMachine (extends StatesMachine<MyEntity>)
```

---

## Core

Core ECS abstractions for Godot 4. Provides the three fundamental building blocks — `Entity`, `Component`, and `System` — as Godot `Node` subclasses so they integrate naturally with the scene tree.

### `Entity`

The base class for all game entities. Extends `CharacterBody3D`, so every entity is a 3D physics body.

Exposes a `Children` property — a live `IReadonlyTypedSet<Node>` of all descendant nodes — so components attached to the entity can be looked up efficiently by type at runtime.

```csharp
[GlobalClass]
public partial class Player : Entity { }
```

```txt
Player  (Entity)
├── Velocity
├── Rotation
└── PlayerStateMachine
```

```csharp
// Look up a sibling component from within another component.
var rotation = base.Entity!.Children.GetOrNull<Rotation>();
```

---

### `Component`

The base class for all components. Extends `Node`, is marked `[GlobalClass]`, and resolves a reference to the owning `Entity` automatically when it enters the scene tree (via `GetOwner<Entity>()`).

It also tracks the direct children of the owning entity, calling `OnSiblingTracked` / `OnSiblingUntracked` as siblings enter or leave — making inter-component communication straightforward without coupling classes together.

```csharp
[GlobalClass]
public partial class MyComponent : Component
{
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        GD.Print(base.Entity!.Name);
    }

    protected override void OnSiblingTracked(Node node)
    {
        if (node is Velocity velocity)
            velocity.ValueChanged += OnVelocityChanged;
    }

    protected override void OnSiblingUntracked(Node node)
    {
        if (node is Velocity velocity)
            velocity.ValueChanged -= OnVelocityChanged;
    }
}
```

| Member                     | Description                                                   |
| -------------------------- | ------------------------------------------------------------- |
| `Entity?`                  | The owning entity; `null` while outside the scene tree.       |
| `OnSiblingTracked(Node)`   | Called when a sibling node is added to the owning entity.     |
| `OnSiblingUntracked(Node)` | Called when a sibling node is removed from the owning entity. |

> **Note:** `Entity` is `null` while the component is outside the tree.

---

### `Component<TValue>`

A typed component that wraps a single value. The value is settable by subclasses and fires a `ValueChanged` event only when the value actually changes (checked via `EqualityComparer<TValue>`).

```csharp
[GlobalClass]
public partial class Speed : Component<float>
{
    public Speed() : base(0f) { }

    public override void _EnterTree()
    {
        base._EnterTree();
        base.ValueChanged += OnSpeedChanged;
    }

    private void OnSpeedChanged(float speed) =>
        GD.Print($"Speed changed to {speed}");
}
```

| Member                               | Description                                              |
| ------------------------------------ | -------------------------------------------------------- |
| `TValue Value`                       | The current value. Read publicly, written by subclasses. |
| `event Action<TValue>? ValueChanged` | Fired when `Value` is assigned a different value.        |

---

### `System`

The base class for all systems. Extends `Node` and automatically tracks every `Entity` present anywhere in the scene tree, maintaining a live `Entities` set. Override `OnEntityTracked` and `OnEntityUntracked` to react as entities appear or disappear.

```csharp
[GlobalClass]
public partial class GravitySystem : System
{
    protected override void OnEntityTracked(Entity entity)
    {
        // React when a new entity enters the scene.
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        foreach (var entity in base.Entities)
        {
            // Process every entity each frame.
        }
    }
}
```

| Member                          | Description                                           |
| ------------------------------- | ----------------------------------------------------- |
| `IReadOnlySet<Entity> Entities` | Live set of all entities currently in the scene tree. |
| `OnEntityTracked(Entity)`       | Called when an entity enters the scene tree.          |
| `OnEntityUntracked(Entity)`     | Called when an entity exits the scene tree.           |

---

## Components

Ready-to-use `Component` implementations that cover the most common 3D game entity behaviours. All components are marked `[GlobalClass]` and are available directly from the Godot editor.

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

### `Area3D`

ECS-aware wrapper around `Godot.Area3D`. Use it when a component needs to be an `Area3D` node and cannot extend `Component` directly. It replicates the full `Component` contract: resolves the owning `Entity` on `_EnterTree` and calls `OnSiblingTracked` / `OnSiblingUntracked` as siblings appear or disappear.

```csharp
[GlobalClass]
public partial class HitBox : Area3D
{
    protected override void OnSiblingTracked(Node node)
    {
        if (node is Health health)
            this.BodyEntered += _ => health.TakeDamage(10f);
    }
}
```

| Member                     | Description                                                   |
| -------------------------- | ------------------------------------------------------------- |
| `Entity?`                  | The owning entity; `null` while outside the scene tree.       |
| `OnSiblingTracked(Node)`   | Called when a sibling node is added to the owning entity.     |
| `OnSiblingUntracked(Node)` | Called when a sibling node is removed from the owning entity. |

---

### `AnimationPlayer`

ECS-aware wrapper around `Godot.AnimationPlayer`. Use it when a component needs to be an `AnimationPlayer` node and cannot extend `Component` directly (C# does not allow multiple inheritance). It replicates the full `Component` contract: resolves the owning `Entity` on `_EnterTree` and calls `OnSiblingTracked` / `OnSiblingUntracked` as siblings appear or disappear.

```csharp
[GlobalClass]
public partial class PlayerAnimator : AnimationPlayer
{
    protected override void OnSiblingTracked(Node node)
    {
        if (node is Velocity velocity)
            velocity.ValueChanged += OnVelocityChanged;
    }

    protected override void OnSiblingUntracked(Node node)
    {
        if (node is Velocity velocity)
            velocity.ValueChanged -= OnVelocityChanged;
    }

    private void OnVelocityChanged(Vector3 v) =>
        this.Play(v != Vector3.Zero ? "run" : "idle");
}
```

| Member                     | Description                                                   |
| -------------------------- | ------------------------------------------------------------- |
| `Entity?`                  | The owning entity; `null` while outside the scene tree.       |
| `OnSiblingTracked(Node)`   | Called when a sibling node is added to the owning entity.     |
| `OnSiblingUntracked(Node)` | Called when a sibling node is removed from the owning entity. |

---

## Utils

Low-level utilities with no dependency on `Core` or `Components`, so they can be used in isolation.

### `ElementsPool`

A static, type-keyed object pool that avoids repeated heap allocations by reusing instances across their lifetime.

```csharp
// Retrieve an instance from the pool (or create one if the pool is empty).
var obj = ElementsPool.GetOrCreate<MyClass>();

// Return an instance back to the pool when done with it.
ElementsPool.Set(obj);
```

Internally uses a `Stack<object>` per type, so both get and return are O(1).

---

### `TypedSet<TElement>`

A set of `TElement` that additionally maintains per-derived-type buckets, allowing efficient lookup of elements by their concrete type without iterating the full collection.

```csharp
var set = new TypedSet<Node>();
set.Add(mySprite);     // Sprite3D : Node3D : Node
set.Add(myLabel);      // Label3D  : Node3D : Node

// Get all Node3D instances (includes both sprite and label).
IReadOnlySet<Node3D> nodes = set.GetAll<Node3D>();

// Get the single Sprite3D (throws if zero or more than one).
Sprite3D sprite = set.Get<Sprite3D>();

// Get the single Label3D, or null if absent.
Label3D? label = set.GetOrNull<Label3D>();
```

Implements both `ISet<TElement>` and `IReadonlyTypedSet<TElement>`. Full set operations (`UnionWith`, `IntersectWith`, `ExceptWith`, `SymmetricExceptWith`) keep the derived-type buckets in sync automatically.

---

### `IReadonlyTypedSet<TElement>`

The read-only surface of `TypedSet<TElement>`. Extends `IReadOnlySet<TElement>` with the three typed-query methods:

| Method                  | Description                                                                                  |
| ----------------------- | -------------------------------------------------------------------------------------------- |
| `GetAll<TDerived>()`    | Returns all elements whose type is `TDerived` or a subtype of it.                            |
| `Get<TDerived>()`       | Returns the single element of type `TDerived`. Throws if not exactly one.                    |
| `GetOrNull<TDerived>()` | Returns the single element of type `TDerived`, or `null` if absent. Throws if more than one. |

---

### `NodesTracker<TNode>`

Watches a Godot node subtree and maintains a live set of all descendant nodes that are of type `TNode`. Fires events when nodes enter or exit the tracked subtree.

```csharp
var tracker = new NodesTracker<CharacterBody3D>();

tracker.NodeTracked   += node => GD.Print($"{node.Name} entered");
tracker.NodeUntracked += node => GD.Print($"{node.Name} exited");

// Start watching from a root node (the root itself is excluded).
tracker.Track(GetTree().Root);

// Live set of all currently tracked nodes.
IReadOnlySet<CharacterBody3D> bodies = tracker.Nodes;

// Stop watching and clean up all event subscriptions.
tracker.Untrack();
```

`Track` and `Untrack` must be called in pairs. Calling `Track` again before `Untrack` throws an `InvalidOperationException`.

**`DirectChildren` mode:**

Set `DirectChildren = true` to restrict tracking to the immediate children of the root, skipping deeper descendants entirely.

```csharp
// Only immediate children of the entity are tracked.
var tracker = new NodesTracker<Node>() { DirectChildren = true };
tracker.Track(myEntity);
```

| Property         | Default | Description                                                              |
| ---------------- | ------- | ------------------------------------------------------------------------ |
| `DirectChildren` | `false` | When `true`, only direct children of the root are tracked; no recursion. |

---

## Releasing

The package is published to GitHub Packages automatically on every tag that matches the pattern `v<major>.<minor>.<patch>` (e.g. `v1.2.0`). Push a tag to trigger the CI/CD pipeline.
