# AGENTS.md

Authoritative guide for AI agents working on this repository. Read the whole file before touching any code.

---

## Project overview

`godot-ecs` is a lightweight ECS framework for [Godot 4](https://godotengine.org/) written in C#. It is distributed as a single NuGet package (`ECS`) and is structured in three layers:

```txt
ECS/
├── Core/        Entity, Component, System — the ECS abstractions.
├── Components/  Ready-to-use Component implementations.
└── Utils/       Low-level utilities with no ECS dependency.
```

**Layer rule:** `Components` → `Core` → `Utils`. A layer may only depend on the layer directly above it. `Utils` has no internal dependencies.

---

## Tooling

| Setting          | Value                 |
| ---------------- | --------------------- |
| Language         | C# 14                 |
| Target framework | .NET 10 (`net10.0`)   |
| Godot SDK        | `Godot.NET.Sdk/4.7.1` |
| Nullable         | enabled (errors)      |
| Implicit usings  | enabled               |
| Solution file    | `ECS.slnx`            |
| Project file     | `ECS/ECS.csproj`      |
| README           | `.github/README.md`   |

---

## Repository layout

```txt
ECS/
├── Core/
│   ├── Entity.cs                    — abstract base for all entities
│   ├── Component.cs                 — base for all components
│   ├── Component.TValue.cs          — generic value-wrapping component
│   └── System.cs                    — base for all systems
├── Components/
│   ├── Main.cs                      — marker component
│   ├── Height.cs                    — float value component
│   ├── Velocity.cs                  — 3D physics velocity
│   ├── Rotation.cs                  — smooth look-at rotation
│   ├── StatesMachine.cs             — generic pooled state machine
│   ├── AnimationPlayer.cs           — Godot AnimationPlayer wrapper
│   ├── Area3D.cs                    — Godot Area3D wrapper
│   └── CollisionShape3D.cs          — Godot CollisionShape3D wrapper
└── Utils/
    ├── ElementsPool.cs              — static type-keyed object pool
    ├── TypedSet.TElement.cs         — set with per-type buckets
    ├── IReadonlyTypedSet.TElement.cs — read-only interface for TypedSet
    └── NodesTracker.TNode.cs        — live subtree watcher
```

Generic classes split into partial files use the naming convention `TypeName.TTypeParam.cs` (e.g., `Component.TValue.cs`, `NodesTracker.TNode.cs`).

---

## Architecture

### `Entity` (`Core/Entity.cs`)

- Extends `Godot.CharacterBody3D`. Every entity is a 3D physics body.
- `abstract` — must always be subclassed; no `[GlobalClass]` on the base.
- Internally creates a `NodesTracker<Node>` (full recursion, `DirectChildren = false`) rooted on itself to populate `Children`, a `TypedSet<Node>` that components query via `Children.Get<T>()`, `Children.GetOrNull<T>()`, and `Children.GetAll<T>()`.

### `Component` (`Core/Component.cs`)

- Extends `Godot.Node`. Marked `[GlobalClass]` so it can be used directly in the Godot editor.
- Resolves `Entity` via `GetOwner<Entity>()` in `_EnterTree`.
- Creates a `NodesTracker<Node>` with `DirectChildren = true` rooted on the owning entity to track siblings. Fires `OnSiblingTracked` / `OnSiblingUntracked` — override these to subscribe/unsubscribe to sibling events without tight coupling.
- `Entity` is `null` outside the scene tree; assert non-null with `!` when inside Godot callbacks.

### `Component<TValue>` (`Core/Component.TValue.cs`)

- `abstract partial` — extends `Component`.
- Wraps a single value with a `ValueChanged` event, fired only when the value actually changes (via `EqualityComparer<TValue>`).
- Uses C# 14 primary constructor: `Component<TValue>(TValue initialValue)`.

### `System` (`Core/System.cs`)

- Extends `Godot.Node`. Marked `[GlobalClass]`.
- Creates a `NodesTracker<Entity>` rooted at `GetTree().Root` (full recursion). Override `OnEntityTracked` / `OnEntityUntracked` to react to entities entering or leaving the scene tree.
- `Entities` exposes the live `IReadOnlySet<Entity>` from the tracker.

### Godot-node wrappers (`Components/AnimationPlayer.cs`, `Area3D.cs`, `CollisionShape3D.cs`)

C# does not support multiple inheritance, so nodes that must extend a specific Godot type (other than `Node`) cannot extend `Component`. These wrapper classes replicate the full `Component` contract manually:

- Resolve `Entity` via `GetOwner<Entity>()` in `_EnterTree`.
- Create a `NodesTracker<Node>` with `DirectChildren = true` rooted on the entity.
- Expose `OnSiblingTracked` / `OnSiblingUntracked` virtual hooks.
- The `_ExitTree` teardown mirrors `Component._ExitTree` exactly.

### `NodesTracker<TNode>` (`Utils/NodesTracker.TNode.cs`)

- `sealed`. Watches a Godot node subtree via `ChildEnteredTree` / `ChildExitingTree` events.
- `Track(root)` / `Untrack()` must be called in pairs; calling either out of order throws `InvalidOperationException`.
- `DirectChildren = true` restricts tracking to immediate children of the root only (no recursion). Used by `Component` and all wrappers to track siblings.
- `DirectChildren = false` (default) recurses the full subtree. Used by `Entity` and `System`.

### `TypedSet<TElement>` / `IReadonlyTypedSet<TElement>` (`Utils/`)

- A `HashSet` augmented with per-concrete-type buckets, enabling O(1) typed lookups.
- Buckets are borrowed from `ElementsPool` and returned when emptied.
- `Entity.Children` exposes this as `IReadonlyTypedSet<Node>`.

### `ElementsPool` (`Utils/ElementsPool.cs`)

- Static, type-keyed pool backed by `Stack<object>` per type.
- Used by `TypedSet` for its per-type `HashSet<T>` buckets and by `StatesMachine` for state instances.

---

## Coding conventions

### Namespaces

| Directory     | Namespace        |
| ------------- | ---------------- |
| `Core/`       | `ECS.Core`       |
| `Components/` | `ECS.Components` |
| `Utils/`      | `ECS.Utils`      |

### Class modifiers

- Always `partial`.
- Use `[GlobalClass]` on every class that Godot users instantiate from the editor (`Component`, `System`, and all `Components/` classes). Do **not** put it on abstract bases (`Entity`, `Component<TValue>`, `StatesMachine<TEntity>`).
- Use `abstract` when a class is only meaningful as a base (`Entity`, `Component<TValue>`, `StatesMachine<TEntity>`, state base classes).
- Use `sealed` for utility classes that must not be subclassed (`NodesTracker<TNode>`, `TypedSet<TElement>`).
- Use `static` only for pure utility classes with no instance state (`ElementsPool`).

### Member access

- Always qualify instance members with `this.` and inherited members with `base.`.
- `protected` for hooks and state that subclasses need (`Entity`, `Entities`, `OnSiblingTracked`, `OnEntityTracked`, `Value` setter, `SetState`).
- `private set` or `protected set` on properties that must not be assigned from outside.
- `private readonly` for all internal fields.

### `_EnterTree` / `_ExitTree` lifecycle order

**`_EnterTree`:**

1. `base._EnterTree()` — always first.
2. Resolve state (e.g., `this.Entity = base.GetOwner<Entity>()`).
3. Subscribe to tracker events (`NodeTracked +=`, `NodeUntracked +=`).
4. Call `tracker.Track(...)`.
5. Subscribe to value-changed events and fire an initial sync call.

**`_ExitTree`:**

1. Fire any cleanup sync calls (e.g., reset velocity to zero).
2. Unsubscribe from value-changed events.
3. Call `tracker.Untrack()` — always before unsubscribing tracker events.
4. Unsubscribe tracker events in **reverse** subscription order (`NodeUntracked -=` before `NodeTracked -=`).
5. Clear state (e.g., `this.Entity = null`, reset fields to zero/default).
6. `base._ExitTree()` — always last.

### Expressions and bodies

- Single-expression methods and property getters use `=>`.
- Multi-statement methods use block bodies `{ }`.
- Empty virtual hooks use an inline empty block: `protected virtual void OnSiblingTracked(Node node) { }`.

### Variables and types

- `var` for local variables; explicit types for fields, properties, and parameters.
- `[]` for empty collection literals (C# 12+ collection expressions).
- Target-typed `new()` for field initializers: `private readonly NodesTracker<Node> _tracker = new() { DirectChildren = true };`.
- Nullable reference types enforced throughout; use `!` assertion only inside Godot callbacks where the lifecycle invariant is guaranteed.

---

## Documentation rules

These rules apply to every public and protected member. Do **not** document private or internal members.

1. Every public/protected member gets an XML `<summary>` doc comment.
2. The description **must fit on a single line** — never wrap across multiple `///` lines.
3. Use `<see cref="..."/>` for types/members, `<typeparamref name="..."/>` for type parameters, `<paramref name="..."/>` for method parameters, and `<c>...</c>` for literals (`null`, `true`, `false`).

**Correct:**

```csharp
/// <summary>
/// Called when a sibling node is added to the owning <see cref="Entity"/>.
/// </summary>
protected virtual void OnSiblingTracked(Node node) { }
```

**Wrong — wraps to a second line:**

```csharp
/// <summary>
/// Called when a sibling node is added to the owning
/// <see cref="Entity"/>.
/// </summary>
```

**Wrong — placeholder not replaced:**

```csharp
/// <summary>
/// // TODO: document this.
/// </summary>
```

---

## Patterns

### Adding a new `Component`

1. Create `ECS/Components/MyComponent.cs`.
2. Extend `Component` (or `Component<TValue>` if a value is needed).
3. Mark `[GlobalClass]` and `partial`.
4. React to siblings in `OnSiblingTracked` / `OnSiblingUntracked` — do not access `Entity.Children` at construction time.
5. Always call `base._EnterTree()` first and `base._ExitTree()` last.
6. Document every public and protected member with a single-line `<summary>`.

```csharp
/// <summary>
/// One-line description of what this component does.
/// </summary>
[GlobalClass]
public partial class MyComponent : Component
{
    protected override void OnSiblingTracked(Node node)
    {
        if (node is Velocity velocity)
            velocity.ValueChanged += this.OnVelocityChanged;
    }

    protected override void OnSiblingUntracked(Node node)
    {
        if (node is Velocity velocity)
            velocity.ValueChanged -= this.OnVelocityChanged;
    }

    private void OnVelocityChanged(Vector3 v) { /* ... */ }
}
```

### Adding a Godot-node wrapper component

Use this when the component must extend a specific Godot type (e.g., `Godot.Area3D`) and cannot inherit `Component`. Copy the boilerplate from an existing wrapper (`Area3D.cs`, `AnimationPlayer.cs`, `CollisionShape3D.cs`) and change only the base class and the class `<summary>`.

```csharp
/// <summary>
/// ECS-aware wrapper around <see cref="Godot.MyNode"/> that resolves the owning <see cref="Entity"/> and tracks sibling nodes.
/// </summary>
[GlobalClass]
public partial class MyWrapper : Godot.MyNode
{
    /// <summary>
    /// The entity that owns this component; <c>null</c> while the component is outside the scene tree.
    /// </summary>
    protected Entity? Entity { get; private set; }

    private readonly NodesTracker<Node> _childrenTracker = new() { DirectChildren = true };

    public override void _EnterTree()
    {
        base._EnterTree();
        this.Entity = base.GetOwner<Entity>();
        this._childrenTracker.NodeTracked += this.OnSiblingTracked;
        this._childrenTracker.NodeUntracked += this.OnSiblingUntracked;
        this._childrenTracker.Track(this.Entity);
    }

    /// <summary>
    /// Called when a sibling node is added to the owning <see cref="Entity"/>.
    /// </summary>
    protected virtual void OnSiblingTracked(Node node) { }

    /// <summary>
    /// Called when a sibling node is removed from the owning <see cref="Entity"/>.
    /// </summary>
    protected virtual void OnSiblingUntracked(Node node) { }

    public override void _ExitTree()
    {
        this._childrenTracker.Untrack();
        this._childrenTracker.NodeUntracked -= this.OnSiblingUntracked;
        this._childrenTracker.NodeTracked -= this.OnSiblingTracked;
        this.Entity = null;
        base._ExitTree();
    }
}
```

### Adding a state to a `StatesMachine`

States are nested `abstract class` types inside the concrete state machine. Use `BaseState` for stateless states and `BaseState<TStateParams>` when data must be passed on transition. `TStateParams` must be a `struct`.

```csharp
public partial class PlayerStateMachine : StatesMachine<Player>
{
    public override void _EnterTree()
    {
        base._EnterTree();
        this.SetState<IdleState, IdleState.Params>(new IdleState.Params());
    }

    public sealed class IdleState : BaseState<IdleState.Params>
    {
        public struct Params { }

        protected override void OnInit() { /* ... */ }
        protected override void OnUpdate(double delta) { /* ... */ }
        protected override void OnDispose() { /* ... */ }
        protected override bool ReadyToTransition() => true;
    }
}
```

- States are pooled via `ElementsPool`; never allocate them with `new` — always call `SetState<TState, TParams>`.
- `force: true` bypasses the `ReadyToTransition()` guard.
- Transitions queued with `SetState` are applied at the start of the next `_PhysicsProcess`.

### Adding a `Utils` class

- No dependency on `ECS.Core` or `ECS.Components`.
- Use `sealed` unless subclassing is the explicit design intent.
- Pool instances with `ElementsPool` when the class has no meaningful constructor parameters and is allocated frequently.

---

## Commit conventions

- Format: `type(Scope): description` — e.g., `feat(Velocity): add Accelerate method`.
- Lowercase `type`: `feat`, `fix`, `refactor`, `docs`, `chore`.
- Scope is the primary class or subsystem affected (PascalCase).
- One logical change per commit.
- **Always update `.github/README.md` in the same commit as the code change it documents.** Never put README changes in a separate commit when they can accompany the code commit.
- To amend a past commit, use `git commit --fixup=<sha>` then `git rebase -i --autosquash <parent-sha>`.
