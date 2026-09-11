# godot-ecs

A lightweight Entity Component System (ECS) framework for [Godot 4](https://godotengine.org/) built in C#. A single NuGet package with Roslyn source generators that eliminate boilerplate.

## Architecture

```txt
ECS/
├── Interfaces/  Role marker interfaces: IEntity, IComponent, ISystem.
├── Core/        Base classes: Entity, Component, System.
├── Components/  Ready-to-use components: velocity, rotation, height, state machine, animation player, collision shape.
├── Entities/    Ready-to-use entity variants: CharacterBody3D, Area3D.
└── Utils/       Low-level utilities: object pooling, typed sets, node tracking.
```

Each layer only depends on the layers above it: `Components`/`Entities` → `Core` → `Interfaces` → `Utils`.

## Getting Started

Add the package via NuGet:

```xml
<PackageReference Include="ECS" Version="0.1.0" />
```

The typical scene setup looks like this:

```txt
MyScene
├── MySystem          (implements ISystem)
└── MyEntity          (implements IEntity)
    ├── Main          (marker component)
    ├── Velocity      (physics velocity)
    ├── Rotation      (look-at rotation)
    └── MyStateMachine (extends StatesMachine<MyEntity>)
```

---

## Interfaces

The framework is driven by three marker interfaces. Implementing them on any Godot `Node` subclass triggers source generators that produce all the necessary boilerplate at compile time.

### `IEntity`

Marker interface for entities. Exposes:

| Member       | Type                            | Description                                |
| ------------ | ------------------------------- | ------------------------------------------ |
| `Components` | `IReadonlyTypedSet<IComponent>` | Live typed set of direct child components. |
| `Children`   | `IReadOnlySet<IEntity>`         | Live set of direct child entities.         |
| `Parent`     | `IEntity?`                      | The parent entity, or `null`.              |

Entity is a pure container — it has no lifecycle hooks (`OnInit`/`OnUpdate`/`OnDispose`).

### `IComponent`

Marker interface for components. The generator produces:

- `Entity` property (`IEntity?`) — the owning entity.
- `Siblings` property (`IReadonlyTypedSet<IComponent>`) — live set of sibling components.
- Lifecycle hooks: `OnInit()`, `OnUpdate(double delta)`, `OnDispose()`.
- Sibling hooks: `OnSiblingTracked(IComponent)`, `OnSiblingUntracked(IComponent)`.

### `ISystem`

Marker interface for systems. The generator produces:

- `Entities` property (`IReadonlyTypedSet<IEntity>`) — live set of all entities in the scene tree.
- Lifecycle hooks: `OnInit()`, `OnUpdate(double delta)`, `OnDispose()`.
- Entity hooks: `OnEntityTracked(IEntity)`, `OnEntityUntracked(IEntity)`.

---

## Core

Ready-to-use base classes that combine a Godot `Node` with the appropriate interface.

### `Entity`

Base class for entities. Extends `Node` and implements `IEntity`.

```csharp
[GlobalClass]
public partial class Player : Entity { }
```

All boilerplate (`Components`, `Children`, `Parent`, trackers) is generated. Access components via:

```csharp
// Look up a component from within another component.
var velocity = base.Entity!.Components.GetOrNull<Velocity>();
```

### `Component`

Base class for all components. Extends `Node` and implements `IComponent`.

```csharp
[GlobalClass]
public partial class MyComponent : Component
{
    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);
        GD.Print(base.Entity!.Name);
    }

    protected override void OnSiblingTracked(IComponent component)
    {
        base.OnSiblingTracked(component);
        if (component is Velocity velocity)
            velocity.ValueChanged += this.OnVelocityChanged;
    }

    protected override void OnSiblingUntracked(IComponent component)
    {
        if (component is Velocity velocity)
            velocity.ValueChanged -= this.OnVelocityChanged;
        base.OnSiblingUntracked(component);
    }

    private void OnVelocityChanged(Vector3 v) { /* ... */ }
}
```

| Member                           | Description                                                  |
| -------------------------------- | ------------------------------------------------------------ |
| `Entity?`                        | The owning entity; `null` while outside the scene tree.      |
| `Siblings`                       | Live typed set of sibling components on the same entity.     |
| `OnInit()`                       | Called once after entering the scene tree (after setup).     |
| `OnUpdate(double)`               | Called every physics frame.                                  |
| `OnDispose()`                    | Called once before exiting the scene tree (before teardown). |
| `OnSiblingTracked(IComponent)`   | Called when a sibling component is added.                    |
| `OnSiblingUntracked(IComponent)` | Called when a sibling component is removed.                  |

> **Note:** `Entity` is `null` outside the scene tree. Inside lifecycle hooks it is guaranteed non-null — use `base.Entity!`.

### `Component<TValue>`

A component that wraps a single value. Fires a `ValueChanged` event when the value changes.

```csharp
[GlobalClass]
public partial class Speed : Component<float>
{
    public Speed() : base(0f) { }

    protected override void OnInit()
    {
        base.OnInit();
        base.Value = 10f;
    }
}
```

| Member         | Description                                                      |
| -------------- | ---------------------------------------------------------------- |
| `Value`        | The current value. Fires `ValueChanged` when set to a new value. |
| `ValueChanged` | Event raised when the value changes.                             |

### `System`

Base class for all systems. Extends `Node` and implements `ISystem`.

```csharp
[GlobalClass]
public partial class MovementSystem : System
{
    protected override void OnEntityTracked(IEntity entity)
    {
        base.OnEntityTracked(entity);
        GD.Print($"Entity entered: {entity.Name}");
    }

    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);
        foreach (var entity in base.Entities)
        {
            var velocity = entity.Components.GetOrNull<Velocity>();
            // process...
        }
    }
}
```

| Member                       | Description                                       |
| ---------------------------- | ------------------------------------------------- |
| `Entities`                   | Live typed set of all entities in the scene tree. |
| `OnInit()`                   | Called once after entering the scene tree.        |
| `OnUpdate(double)`           | Called every physics frame.                       |
| `OnDispose()`                | Called once before exiting the scene tree.        |
| `OnEntityTracked(IEntity)`   | Called when an entity enters the scene tree.      |
| `OnEntityUntracked(IEntity)` | Called when an entity exits the scene tree.       |

---

## Entity variants (`Entities/`)

Ready-to-use entity implementations based on specific Godot node types. Implement `IEntity` — all boilerplate is generated.

| Class             | Extends                 |
| ----------------- | ----------------------- |
| `CharacterBody3D` | `Godot.CharacterBody3D` |
| `Area3D`          | `Godot.Area3D`          |

```csharp
// Add your own entity variant:
[GlobalClass]
[HideInheritedMembers("Name")]
public partial class RigidBody3D : Godot.RigidBody3D, IEntity
{
    string INode.Name => base.Name;
}
```

---

## Components

### `Main`

Marker component used to tag an entity as the primary entity of its scene.

### `Height`

Component that stores the entity’s height in metres as an inspector-editable value.

### `Velocity`

Component that drives 3D physics velocity, applying gravity, air friction, and a max speed limit.

| Member         | Description                     |
| -------------- | ------------------------------- |
| `Gravity`      | Downward acceleration (m/s²).   |
| `AirFriction`  | Horizontal speed loss (m/s²).   |
| `MaxSpeed`     | Maximum horizontal speed (m/s). |
| `Accelerate()` | Apply an acceleration impulse.  |
| `Decelerate()` | Apply a deceleration impulse.   |

> Casts `Entity` to `CharacterBody3D` internally to call Godot physics methods.

### `Rotation`

Component that smoothly rotates the entity to face a world-space target position each physics frame.

| Member         | Description                   |
| -------------- | ----------------------------- |
| `AngularSpeed` | Rotation speed (°/s).         |
| `Target`       | World-space position to face. |

> Casts `Entity` to `Node3D` internally to read/write `GlobalPosition` and `Rotation`.

### `StatesMachine<TEntity>`

Generic finite state machine component that pools states and processes queued transitions each physics frame.

```csharp
public partial class PlayerStateMachine : StatesMachine<Player>
{
    protected override void OnInit()
    {
        base.OnInit();
        this.SetState<IdleState, IdleState.Params>(new IdleState.Params());
    }

    public sealed class IdleState : BaseState<IdleState.Params>
    {
        public struct Params { }

        protected override void OnInit()
        {
            base.OnInit();
            // subscribe to events...
        }

        protected override void OnSiblingTracked(IComponent component)
        {
            if (component is Velocity velocity)
                velocity.ValueChanged += this.OnVelocityChanged;
        }

        protected override void OnSiblingUntracked(IComponent component)
        {
            if (component is Velocity velocity)
                velocity.ValueChanged -= this.OnVelocityChanged;
        }

        protected override void OnUpdate(double delta) { /* ... */ }

        protected override void OnDispose()
        {
            // clean up...
            base.OnDispose();
        }

        protected override bool ReadyToTransition() => true;

        private void OnVelocityChanged(Vector3 v) { /* ... */ }
    }
}
```

| Member                        | Description                               |
| ----------------------------- | ----------------------------------------- |
| `SetState<TState, TParams>()` | Enqueue a state transition.               |
| `Value`                       | The currently active state (or `null`).   |
| `ReadyToTransition()`         | Override to block non-forced transitions. |

States are pooled via `ElementsPool` — never instantiate them with `new`. Always transition via `SetState`. Use `force: true` to bypass `ReadyToTransition()`.

### `AnimationPlayer`

Godot `AnimationPlayer` wrapper that implements `IComponent`. All boilerplate is generated.

### `CollisionShape3D`

Godot `CollisionShape3D` wrapper that implements `IComponent`. All boilerplate is generated.

---

## Utils

### `ElementsPool`

A static, type-keyed object pool that avoids repeated heap allocations.

```csharp
var obj = ElementsPool.GetOrCreate<MyClass>();
ElementsPool.Set(obj);
```

### `TypedSet<TElement>`

A set that maintains per-derived-type buckets for efficient typed lookups. Implements `ISet<TElement>` and `IReadonlyTypedSet<TElement>`.

```csharp
var set = new TypedSet<IComponent>();
set.Add(myVelocity);
set.Add(myRotation);

Velocity? v    = set.GetOrNull<Velocity>();
Velocity  v2   = set.Get<Velocity>();
IReadOnlySet<IComponent> all = set.GetAll<IComponent>();
```

### `IReadonlyTypedSet<TElement>`

Read-only view of `TypedSet<TElement>`. Extends `IReadOnlySet<TElement>` with:

| Method                  | Description                                                                          |
| ----------------------- | ------------------------------------------------------------------------------------ |
| `GetAll<TDerived>()`    | All elements whose type is `TDerived` or a subtype.                                  |
| `Get<TDerived>()`       | The single element of type `TDerived`. Throws if not exactly one.                    |
| `GetOrNull<TDerived>()` | The single element of type `TDerived`, or `null` if absent. Throws if more than one. |

### `NodesTracker<TNode>`

Watches a Godot node subtree and maintains a live set of nodes matching type `TNode`.
Constraint: `where TNode : INode` — supports both classes and interfaces (e.g., `IEntity`, `IComponent`).

```csharp
var tracker = new NodesTracker<IEntity>();

tracker.NodeTracked   += entity => GD.Print($"tracked");
tracker.NodeUntracked += entity => GD.Print($"untracked");

// Start watching from a root node (the root itself is excluded).
tracker.Track(GetTree().Root);

// Live set of all currently tracked nodes.
IReadOnlySet<IEntity> entities = tracker.Nodes;

// Stop watching and clean up all event subscriptions.
tracker.Untrack();
```

`Track` and `Untrack` must be called in pairs.

| Property         | Default | Description                                                              |
| ---------------- | ------- | ------------------------------------------------------------------------ |
| `DirectChildren` | `false` | When `true`, only direct children of the root are tracked; no recursion. |

---

## Source generators

The `ECS.Generators` project contains Roslyn incremental source generators that run at compile time. Wired into `ECS.csproj` via `ProjectReference` with `OutputItemType="Analyzer"`. Not shipped as a NuGet package.

| Generator                       | Trigger                  | Purpose                                                                     |
| ------------------------------- | ------------------------ | --------------------------------------------------------------------------- |
| `HideInheritedMembersGenerator` | `[HideInheritedMembers]` | Hides/seals all inherited Godot public members from IntelliSense            |
| `EntityGenerator`               | `IEntity`                | Generates `Components`, `Children`, `Parent`, trackers, lifecycle overrides |
| `ComponentGenerator`            | `IComponent`             | Generates `Entity`, `Siblings`, trackers, lifecycle overrides + hooks       |
| `SystemGenerator`               | `ISystem`                | Generates `Entities`, tracker, lifecycle overrides + hooks                  |

All members are generated only when absent from the hand-written class (skip-if-present rule).

---

## Releasing

The package is published to GitHub Packages automatically on every tag that matches the pattern `v<major>.<minor>.<patch>` (e.g. `v1.2.0`). Push a tag to trigger the CI/CD pipeline.
