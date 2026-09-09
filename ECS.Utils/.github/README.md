# ECS.Utils

Low-level utilities used by the ECS framework. These classes are general-purpose and have no dependency on `ECS.Core` or `ECS.Components`, so they can be used in isolation.

## Classes

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
