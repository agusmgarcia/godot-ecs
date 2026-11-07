# Godot ECS

A lightweight Entity Component System (ECS) framework for **Godot 4** with **C#**. It provides core utilities (object pooling, reactive node tracking, raycasting helpers) and a set of ready-to-use components (velocity, height, weight, model handling, collision-shape auto-scaling, animation blending, and a pooled finite state machine).

## Features

- **Node-based components** — every component is a regular Godot `Node` (or `Node3D` / `CollisionShape3D` / `AnimationPlayer`) marked with `[GlobalClass]`, so it can be added and configured directly in the Godot editor inspector.
- **Reactive sibling & child tracking** — `TrackSiblings<T>()` and `TrackChildren<T>()` extension methods let any node react to related components entering or leaving the scene tree, enabling fully decoupled, composition-based entity design.
- **Object pooling** — `ElementsPool` provides a static, type-keyed pool to reuse node instances (e.g. states) and avoid repeated heap allocations at runtime.
- **Pooled finite state machine** — `StateMachine` + `State<TStateParams>` implement a queued, pooled FSM that transitions between states each physics frame, with support for forced transitions and typed parameter structs.
- **Physics-ready Velocity component** — applies translation (with gravity) and rotation on a `CharacterBody3D` every physics frame, with inspector-editable max speeds and programmatic `Accelerate` / `Decelerate` API.
- **Auto-scaling collision shapes & animation blending** — components that automatically adjust collision shape dimensions and model transforms based on the entity's `Height` component.
- **FBX armature fix script** — a GDScript `EditorScenePostImport` tool that flattens the extra `Armature` node Blender's FBX exporter creates, preventing broken `AnimationPlayer` track paths.
- **CI/CD** — GitHub Actions workflow that builds, tests, and publishes releases as downloadable `addons/ECS` artifacts on every semver tag push.

## Installation

1. Go to the [Releases](https://github.com/agusmgarcia/godot-ecs/releases) page.
2. Download the artifact from the latest release.
3. Extract the `addons/ECS` folder into your Godot project's `addons/` directory.

## Components

### Main

A marker `Node` that identifies the player-controlled entity. Attach it to distinguish the "main" entity from NPCs or other actors.

### Height

Stores the entity's height in metres via an inspector-editable `[Export]` property. Other components (`ModelHandler`, `AutoTransformCollisionShape3D`) react to it automatically through sibling tracking.

### Weight

Stores the entity's weight in kilograms as an inspector-editable `[Export]` property.

### Velocity

Drives `CharacterBody3D` translation and rotation each physics frame. Provides:

- `MaxSpeed` / `MaxAngularSpeed` / `GravityMaxSpeed` — inspector-editable limits.
- `Accelerate(direction, acceleration, angularAcceleration)` — apply an acceleration impulse.
- `Decelerate(deceleration, lookAtDirection, angularAcceleration)` — apply a deceleration impulse while rotating toward a direction.
- Built-in gravity that applies downward velocity when the character is not on the floor.
- Respects `CharacterBody3D` axis lock settings for rotation.

### ModelHandler

A `Node3D` that manages swappable 3D model children, keeping exactly one visible at a time. It:

- Fires a `ModelChanged` event whenever the active model switches.
- Auto-adjusts its vertical position and scale using a sibling `Height` component.
- Derives model type keys from child node names (strips the `"Model"` suffix by default; override `GetModelType` for custom logic).

### AutoTransformCollisionShape3D

A `CollisionShape3D` that automatically scales its `CapsuleShape3D` or `CylinderShape3D` to match a sibling `Height` component. Height and radius are scaled proportionally from their initial values.

### AutoTransformAnimationPlayer

An `AnimationPlayer` that smoothly blends a sibling `ModelHandler`'s active model position and rotation when animations start/finish. Override `GetTargetPosition` and `GetTargetRotation` to define per-animation offsets.

### StateMachine

A pooled finite state machine implemented as a `Node`. It:

- Manages a single active `State` child.
- Queues transitions and flushes them each physics frame.
- Pools removed states via `ElementsPool` to avoid allocations.
- Supports forced transitions (bypassing `_ReadyToTransition()` guards).
- When transitioning to the same state type, copies new params without a full remove/add cycle.

### State / State\<TStateParams\>

- `State` — abstract base `Node` for FSM states. Override `_ReadyToTransition()` to block non-forced transitions. Sets `Owner` on enter and clears it on exit.
- `State<TStateParams>` — generic subclass that stores a typed `StateParams` struct, automatically copied on each transition via `_CopyStateParams`.

## Systems

### TimeSystem

A `[Tool]` `Node` that maintains an in-game clock and advances it every `_Process` frame. It provides:

- `Value` — the current time in seconds from midnight (0–86399), wrapping automatically at the day boundary.
- `Scale` — a multiplier controlling how fast in-game time passes relative to real time (e.g. `3600` = one real second equals one in-game hour).
- `ValueChanged` — event raised every frame with the new time value after it changes.
- `ForwardToTime(hours, minutes, speed)` — fast-forwards the clock to a specific time of day at a custom speed, then resumes normal flow.

### DayNightSystem

A `[Tool]` `Node` that drives a sibling `DirectionalLight3D` and `WorldEnvironment`'s `ProceduralSkyMaterial` through four configurable time-of-day phases, reacting to a sibling `TimeSystem`'s `ValueChanged` event. Each phase (Sunrise, Day, Sunset, Night) exposes inspector-editable properties for the transition start time, sky horizon colour, sun/moon light colour, and light energy. Transitions between phases use a skewed lerp for a more natural-looking sky progression.

## Utilities

### ElementsPool

A static, type-keyed object pool (`Dictionary<Type, Stack<object>>`). Used by `StateMachine` to recycle `State` instances.

```csharp
var state = ElementsPool.GetOrCreate<MyState>(); // reuse or new
ElementsPool.Set(state);                         // return to pool
```

### NodeExtensions

Extension methods for `Godot.Node`:

- **`TrackSiblings<T>(onTracked, onUntracked, ...)`** — reactively subscribe to sibling nodes of type `T` entering/leaving the owner's subtree.
- **`TrackChildren<T>(onTracked, onUntracked, ...)`** — same but for child nodes of the calling node.
- **`IntersectRay(to, collisionMask, collideWithAreas, excludeList)`** — casts a 3D physics ray from the node's global position and returns the first hit as an `IntersectRayResult` record (collider, normal, position), or `null`.

Options for tracking: `name` filter, `direct` children only, `unique` (throw if more than one match).

## Scripts

### fix_armature_hierarchy.gd

A `@tool` `EditorScenePostImport` script that removes the extra `Armature` node Blender's FBX exporter wraps around skeletons. This flattens the hierarchy from `Scene / Armature / Skeleton3D / ...` to `Scene / Skeleton3D / ...`, preserving the `Armature` transform and preventing broken `AnimationPlayer` track paths.

**Usage:**

1. Select the imported `.fbx` file in the FileSystem dock.
2. In the Import dock, set **Import Script** to this file.
3. Click **Reimport**.

## Author

**Agus Garcia**

## License

This project does not currently include a license file. All rights are reserved by the author unless otherwise specified.
