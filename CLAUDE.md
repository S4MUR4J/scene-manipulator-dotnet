# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build                        # build solution
dotnet test                         # run all tests
dotnet test --verbosity detailed    # run tests with output
dotnet test --filter "FullyQualifiedName~TestName"  # run single test
dotnet build --configuration Release
```

## Architecture

**Entity Component System (ECS)** for managing 3D scenes. Three main layers:

- `Scene` — container for `Entity` instances, keyed by string ID
- `Entity` — holds `IComponent` instances, keyed by component type name
- `IComponent` — data-only records; each has a `string Type` property matching its type name

**Mutation is internal, query is public.** `Entity` and `Scene` expose read-only query methods (`Get<T>`, `Has<T>`, `GetEntity`, etc.) publicly. Mutation methods (`Set`, `Remove`, `AddEntity`, etc.) are `internal` — only writable from within `Manipulator.Core`.

**Component access pattern:**
```csharp
// Generic (type-safe)
var transform = entity.Get<Transform>();
bool has = entity.Has<Transform>();

// String-keyed (for dynamic/deserialization use)
var component = entity.Get("Transform");
```

**ComponentRegistry** maps string type names → `Type` objects. Used for dynamic component resolution (e.g., deserialization). Register via `Register<T>()` or `Register(string, Type)`.

**Vector3** is a `readonly record struct` — immutable, value semantics. Supports `+`, `-`, `*` operators.

## Components

Built-in components in `Models/Components/`:
- `Transform` — Position, Rotation, Scale (all `Vector3`, default to Zero/Zero/One)
- `EntityName` — string label for an entity
- `MeshFilter` — geometry type (`GeometryType` enum) + optional parameters dict
- `MeshRenderer` — Color (hex string), Opacity, Metalness, Roughness

`GeometryType` enum: `Cube, Sphere, Cylinder, Plane, Torus, Pyramid`