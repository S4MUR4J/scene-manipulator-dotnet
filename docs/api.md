# API Reference

This is the reference for talking to the Manipulator API: auth, versioning, error
format, endpoints, and the JSON contract for scene content. It's written for the
Godot client, for coding agents working in this repo, and for thesis chapter 4.

For the complete, generated schema (request/response shapes, status codes), see
[`docs/openapi.json`](./openapi.json) or the live `/openapi/v{version}.json`
endpoint served by the API in development. This document explains the contract
in prose; the OpenAPI document is the source of truth for exact shapes.

## Base URL & versioning

All routes are versioned via a URL segment:

```
/api/v{version}/...
```

Currently only `v1` exists, e.g. `/api/v1/scenes`. The version is read from the
URL segment, not a header or query string.

## Authentication

Every request must include an API key header:

```
X-Api-Key: <key>
```

- Missing header → `401 Unauthorized`, plain-text body `Unauthorized: Missing API key.`
- Wrong key → `401 Unauthorized`, plain-text body `Unauthorized: Invalid API key.`

There is no per-user auth or scoping — one shared key for the whole API. The key
is configured server-side via `Authentication:ApiKey` (see `appsettings.json` /
the `Authentication__ApiKey` environment variable).

## Error format

The API does not use one single error envelope everywhere; the shape depends on
what rejected the request:

| Situation | Status | Body |
|---|---|---|
| Missing/invalid `X-Api-Key` | 401 | Plain text (see above) |
| Request body fails validation (e.g. `POST /scenes` with an empty `name`) | 400 | RFC 7807 `application/problem+json`, via ASP.NET Core's `ValidationProblem` |
| Request body isn't valid JSON | 400 | RFC 7807 `application/problem+json` (ASP.NET Core's default model-binding failure response) |
| Scene (or scene content) doesn't exist | 404 | Empty body |

Example validation error (400):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["'Name' must not be empty."]
  }
}
```

## Endpoints

All paths below are relative to `/api/v1`. All require the `X-Api-Key` header.

### Scene CRUD

| Method | Path | Description | Success | Notes |
|---|---|---|---|---|
| GET | `/scenes` | List all scenes | 200, `SceneRes[]` | |
| GET | `/scenes/{id}` | Get one scene | 200, `SceneRes` / 404 | |
| POST | `/scenes` | Create a scene | 201, `SceneRes` | Body: `{ "name": string }` |
| PUT | `/scenes/{id}` | Rename a scene | 204 / 404 | Body: `{ "name": string }` |
| DELETE | `/scenes/{id}` | Delete a scene | 204 / 404 | |

`SceneRes` shape:

```json
{
  "name": "My Scene",
  "created": "2026-09-01T12:00:00Z",
  "updated": "2026-09-20T08:30:00Z"
}
```

> **Note:** `SceneRes` does not currently include the scene `id`. Callers must
> already know the id (e.g. from the URL they used to create/fetch it, or from
> wherever the id was minted) — the list endpoint alone doesn't let you map a
> row back to an id to fetch its content.

`{id}` is the scene's GUID, e.g. `3fa85f64-5717-4562-b3fc-2c963f66afa6`.

### Scene content

Scene content (the actual ECS scene graph) is a separate sub-resource from the
scene's metadata (name/timestamps), so it can be read and replaced independently.

| Method | Path | Description | Success | Notes |
|---|---|---|---|---|
| GET | `/scenes/{id}/content` | Get a scene's content | 200, `string` / 404 | Body is the raw content string, JSON-encoded (a JSON string, not an object) |
| PUT | `/scenes/{id}/content` | Replace a scene's content | 204 / 404 | Body: `{ "content": string }` |

The `content` value is expected to be a JSON string conforming to the **scene
content JSON contract** below. The API currently stores and returns this value
as an opaque string — it does not itself parse or validate the scene graph
inside it; that's done client-side (see `Manipulator.Core.Serialization.SceneSerializer`)
or by whatever produced it (e.g. `Manipulator.Runner`).

## Scene content JSON contract

This is the format the `content` field holds, and the format
[MAN-45](https://linear.app/manipulator/issue/MAN-45) (Godot scene DTOs) parses.
The canonical machine-readable schema lives in
[`docs/scene.schema.json`](./scene.schema.json); this section is the prose
version plus a worked example.

### Shape

```
{
  "version":       string,   // schema version, currently "1.0"
  "scene_version": integer,  // monotonically increasing, bumped on each mutation
  "entities": [
    {
      "id": string,
      "components": {
        "<component_type>": { ...fields }
      }
    }
  ]
}
```

- **Casing:** every key — top-level fields, component type names, and fields
  inside a component — is `snake_case`. This is produced by
  `JsonNamingPolicy.SnakeCaseLower` in `SceneSerializer`, not hand-picked names.
- **Component keys:** each entry in `components` is keyed by the component's
  type name in `snake_case` (`transform`, not `Transform`). Component type
  matching is exact/case-sensitive; unknown keys are not an error — they're
  dropped and reported as a warning.
- **`entities[].id`:** a string, not a GUID — this is the ECS entity id (see
  `SceneBuilder.Id()` in tests for the convention used when generating them),
  independent of the scene's own GUID `id` in the CRUD API above.
- **`scene_version`:** corresponds to `Scene.Version` in `Manipulator.Core` —
  it's what `VersionConflictValidator` checks against a command's
  `ExpectedVersion` in the command layer.

### Known component types

| Type key | Fields | Notes |
|---|---|---|
| `transform` | `position`, `rotation`, `scale` — each `[x, y, z]` floats | Missing fields default to `Zero` (position/rotation) or `One` (scale). Values must be finite. |
| `mesh_filter` | `geometry` (string), `parameters` (optional object) | `geometry` is one of `GeometryType`: `Cube, Sphere, Cylinder, Cone, Capsule, Plane, Torus, Hemisphere`. Matched case-insensitively (`"cube"` and `"Cube"` both work). `parameters` is a free-form dict, e.g. `{ "radius": 1.5, "segments": 32 }`. |
| `mesh_renderer` | `color` (hex string), `opacity`, `metalness`, `roughness` (floats) | Defaults: `color: "#ffffff"`, `opacity: 1.0`, `metalness: 0.0`, `roughness: 0.5`. Validated (e.g. ranges) by `MeshRendererValidator`. |
| `entity_name` | `value` (string) | Defaults to `""` if absent. |

Unknown component types, and unknown fields within a known component type, are
ignored rather than rejected — the deserializer collects them as warnings
instead of failing the whole scene. Missing required structural fields
(`entities`, an entity's `id`, an entity's `components`) are hard errors.

### Full example

```json
{
  "version": "1.0",
  "scene_version": 2,
  "entities": [
    {
      "id": "entity_1",
      "components": {
        "transform": {
          "position": [0, 1, 0],
          "rotation": [0, 0, 0],
          "scale": [1, 1, 1]
        },
        "mesh_filter": {
          "geometry": "Sphere",
          "parameters": { "radius": 1.5, "segments": 32 }
        },
        "mesh_renderer": {
          "color": "#ff0000",
          "opacity": 1.0,
          "metalness": 0.1,
          "roughness": 0.8
        },
        "entity_name": {
          "value": "RedSphere"
        }
      }
    },
    {
      "id": "entity_2",
      "components": {
        "transform": {
          "position": [2, 0, -3],
          "rotation": [0, 90, 0],
          "scale": [1, 1, 1]
        },
        "mesh_filter": {
          "geometry": "cube"
        },
        "entity_name": {
          "value": "PlainCube"
        }
      }
    }
  ]
}
```

(Same file as `schemas/fixtures/scene.sample.json` — kept in sync as a live
fixture used by tests.)
