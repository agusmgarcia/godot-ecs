# ECS.Core

Core ECS abstractions for Godot 4. Provides the three fundamental building blocks — `Entity`, `Component`, and `System` — as Godot `Node` subclasses so they integrate naturally with the scene tree.

## Classes

### `Entity`

The base class for all game entities. Extends `CharacterBody3D`, so every entity is a 3D physics body.

Exposes a `Children` property — a live `IReadonlyTypedSet<Node>` of all descendant nodes — so components attached to the entity can be looked up efficiently by type at runtime.

```csharp
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

The base class for all components. Extends `Node` and resolves a reference to the owning `Entity` automatically when it enters the scene tree (via `GetOwner<Entity>()`).

```csharp
[GlobalClass]
public partial class MyComponent : Component
{
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        GD.Print(base.Entity!.Name);
    }
}
```

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
