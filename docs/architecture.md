# Architecture

Simplified technical overview of the system. This is working documentation — coding
agents and reviewers should keep it current — and doubles as the basis for the thesis'
architecture chapter.

## Components

| Component | Responsibility |
|---|---|
| **Godot client** | 3D viewport for humans. Loads a scene from `Manipulator.Api`, renders entities, and (in later scope) sends edits back. Not yet implemented in this repo — a planned external client. |
| **`Manipulator.Core`** | Pure C# library: the ECS scene model (`Scene`, `Entity`, `IComponent`), the `CommandDispatcher`/`ICommandHandler` mutation pipeline, the `EventBus`, and `SceneSerializer` (scene ⇄ JSON, contract in `schemas/scene.schema.json`). No I/O, no network dependency — embeddable in any host. |
| **`Manipulator.Api`** | ASP.NET Core REST API. Owns scene persistence: CRUD over scene metadata and scene content, backed by PostgreSQL via EF Core. Versioned routes (`/api/v{version}/...`), API-key middleware on every request. |
| **PostgreSQL** | Durable storage for scenes (metadata + serialized content) owned exclusively by `Manipulator.Api`. |

`Manipulator.Core` is referenced by `Manipulator.Api`, but today the API stores scene
`Content` as an opaque string — it does not yet deserialize or validate it through
`SceneSerializer`. The ECS pipeline (`CommandDispatcher`, `EventBus`, live mutation) is
currently exercised by a separate host, `Manipulator.Mcp` + `Manipulator.Runner`, which
runs an in-memory `Scene` and exposes it to AI agents over MCP (Streamable HTTP) for
research/benchmarking. It has no database and no connection to `Manipulator.Api` — the
two hosts share the `Core` library, not runtime state. That path is out of scope for the
diagram below; see `dev/manipulator_design.md` for the fuller design history.

## Component diagram

```mermaid
graph LR
    Godot["Godot client<br/><i>(planned)</i>"] -->|HTTP + X-Api-Key| Api["Manipulator.Api<br/><i>REST, versioned</i>"]
    Api -->|EF Core| Postgres[("PostgreSQL")]
    Api -.->|references| Core["Manipulator.Core<br/><i>ECS scene model</i>"]
```

## Request flow: loading a scene

```mermaid
sequenceDiagram
    participant Godot as Godot client
    participant Api as Manipulator.Api
    participant DB as PostgreSQL

    Godot->>Api: GET /api/v1/scenes/{id}<br/>X-Api-Key: ...
    Api->>Api: ApiKeyAuthMiddleware validates key
    Api->>DB: SELECT scene by id
    DB-->>Api: name, created, updated
    Api-->>Godot: 200 OK (metadata)

    Godot->>Api: GET /api/v1/scenes/{id}/content/
    Api->>DB: SELECT content by id
    DB-->>Api: content (scene JSON string)
    Api-->>Godot: 200 OK { content }

    Godot->>Godot: parse content per the scene format<br/>(schemas/scene.schema.json)
    Godot->>Godot: render entities in the 3D viewport
```

## Related documents

- `dev/manipulator_design.md` — original design document; broader in scope than this file, partly superseded by the current implementation.
- `schemas/scene.schema.json` — JSON Schema for the scene content contract.
- `docs/api.md` (planned) — endpoint reference, auth, versioning, scene content contract.
- `docs/adr/` (planned) — architectural decision records.
