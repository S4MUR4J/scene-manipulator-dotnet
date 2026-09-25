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

## MCP server

`Manipulator.Mcp` exposes the command set to an LLM agent as MCP tools over streamable HTTP
(official C# SDK, `ModelContextProtocol.AspNetCore`). It holds an in-memory `Scene` and
`CommandDispatcher` and touches no database — `Manipulator.Api` still owns Postgres.

```bash
dotnet run --project Manipulator.Mcp -- \
  --mode mcp --run-id scenario-3-mcp-opus-7 \
  --starting-scene scenes/kitchen.json --call-log runs/run-7.jsonl
```

**One process is one run.** The transport is stateless from protocol revision 2026-07-28 (no
`Mcp-Session-Id`), so a tool call carries nothing that identifies a client; `SessionProvider` holds
a single run-scoped `SceneSession` and the harness gets isolation by starting a server per run.

**Tools** (`mcp` mode): `add_entity`, `move_entity`, `rotate_entity`, `scale_entity`,
`set_material`, `rename_entity`, `remove_entity`, plus the reads `get_scene` and `get_entity` and
`finish`, which ends the run. `dsl` and `text` modes land in their own issues; an unknown `--mode`
fails at startup.

**Every tool returns JSON, never an exception** — `{"ok":true,...}` or
`{"ok":false,"error":"..."}` carrying the validator's or handler's own message so the agent can
correct itself. Tool bodies go through `ToolGateway`, which logs the call and renders the envelope.

**Tool names, descriptions and parameter schemas are controlled constants** of the experiment. All
wording lives in `Tools/ToolDescriptions.cs` and the published schema is pinned by
`Manipulator.Mcp.Tests/Fixtures/tools.snapshot.json`. Regenerate it deliberately:
`MANIPULATOR_UPDATE_SNAPSHOT=1 dotnet test`. Tool methods must not take the SDK's `RequestContext`
— it has been observed leaking into a published schema as a `context` parameter.

**Call log** (`Logging/CallLog.cs`) records the run as one ordered stream for the harness: every
tool call with arguments, result, duration and scene version delta; every `EventBus` write event;
and session start/finish with the final scene. Reads and failures publish no events, which is why
calls are logged too. It is written as JSONL to `--call-log` and served at `GET /session/log`,
alongside `GET /session` and `GET /session/scene`.

## Testing

**Entity ID pattern:** Use `SceneBuilder.Id(index, tag?)` for entity IDs in tests — never raw strings.
- `SceneBuilder.Id(1)` → `"entity_1"`
- `SceneBuilder.Id(1, "player")` → `"player_1"`

`SceneBuilder.Id()` delegates to `TestUtils.Tag(tag, suffix)` in `Manipulator.Core.Tests/Helpers/`.