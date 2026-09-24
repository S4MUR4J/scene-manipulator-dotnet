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

**ComponentRegistry** maps string type names → `Type` objects. Used for dynamic component resolution (e.g., deserialization). Register via `Register<T>()`.

**Vector3** is a `readonly record struct` — immutable, value semantics. Supports `+`, `-`, `*` operators. Direction constants: `Zero, One, Up, Down, Left, Right, Forward, Back`.

## Components

Built-in components in `Manipulator.Core/Ecs/Components/`:
- `Transform` — Position, Rotation, Scale (all `Vector3`, default to Zero/Zero/One)
- `EntityName` — string label for an entity
- `MeshFilter` — geometry type (`GeometryType` enum) + optional parameters dict
- `MeshRenderer` — Color (hex string), Opacity, Metalness, Roughness

`GeometryType` enum: `Cube, Sphere, Cylinder, Cone, Capsule, Plane, Torus, Hemisphere`

## Command layer

Commands live in `Manipulator.Core/Commands/`. `CommandDispatcher.Register<T>` wires a command's string `Type` to an `ICommandHandler<T>` plus a list of `ICommandValidator`s. `Dispatch`:

1. Looks up the handler by `command.Type` (string-keyed, not CLR type, so commands can arrive from JSON with no compile-time coupling).
2. Runs validators in order (`VersionConflictValidator` checks `ICommand.ExpectedVersion` against `Scene.Version`, `EntityExistsValidator` checks the target entity exists); the first failure short-circuits with a failed `CommandResult` and the handler never runs.
3. Invokes the handler, catching exceptions into a failed `CommandResult` so callers never see an unhandled exception.
4. On success, publishes each event in `CommandResult.Events` to the `EventBus`; events are never published for a failed result.

To add a command: define an `ICommand` record with a `Type` string, add an `ICommandHandler<T>` in `Commands/Handlers/`, add any `ICommandValidator`s it needs in `Commands/Validation/`, and register all three via `CommandDispatcher.Register<T>`.

`EventBus` (`Manipulator.Core/Events/EventBus.cs`) is a simple pub/sub keyed by event `Type` (matches subtypes too via `IsInstanceOfType`). Subscriber exceptions are swallowed so one broken subscriber can't block delivery to others.

## Testing

**Entity ID pattern:** Use `SceneBuilder.Id(index, tag?)` for entity IDs in tests — never raw strings.
- `SceneBuilder.Id(1)` → `"entity_1"`
- `SceneBuilder.Id(1, "player")` → `"player_1"`

`SceneBuilder.Id()` delegates to `TestUtils.Tag(tag, suffix)` in `Manipulator.Core.Tests/Helpers/`.