# godot-ecs

A lightweight Entity Component System (ECS) framework for [Godot 4](https://godotengine.org/) built in C#. It is structured as three independent NuGet packages that can be consumed separately depending on how much of the framework you need.

## Packages

| Package                                               | Description                                                             |
| ----------------------------------------------------- | ----------------------------------------------------------------------- |
| [`ECS.Utils`](/ECS.Utils/.github/README.md)           | Low-level utilities: object pooling, typed sets, and node tracking.     |
| [`ECS.Core`](/ECS.Core/.github/README.md)             | Core ECS abstractions: `Entity`, `Component`, and `System`.             |
| [`ECS.Components`](/ECS.Components/.github/README.md) | Ready-to-use components: velocity, rotation, height, and state machine. |

## Architecture

```txt
ECS.Utils
    └── ECS.Core
            └── ECS.Components
```

Each package only depends on the one above it, so you can take `ECS.Utils` or `ECS.Core` standalone without pulling in the full component library.

## Getting Started

Add the packages you need via NuGet:

```xml
<PackageReference Include="ECS.Components" Version="0.1.0" />
```

Or, if you only need the core abstractions:

```xml
<PackageReference Include="ECS.Core" Version="0.1.0" />
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

## Releasing

Packages are published to GitHub Packages automatically on every tag that matches the pattern `<package>@v<major>.<minor>.<patch>` (e.g. `ECS.Core@v1.2.0`). Push a tag to trigger the CI/CD pipeline.
