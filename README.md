# scene-manipulator-dotnet

ECS-based 3D scene library with a unified command interface, so the same scene can be driven by an LLM (via MCP), a REST API, or any other controller. Includes a research runner that benchmarks AI models on scene-building scenarios and scores the results against declarative requirements.

> Under active development.

## Features

- **ECS core** — entities and data-only components, read-only public queries, internal mutation
- **Command pipeline** — string-typed commands, validators (version conflict, entity exists), handlers and an event bus
- **REST API** — scene CRUD and commands over HTTP, PostgreSQL persistence, API-key auth ([OpenAPI spec](docs/openapi.json))
- **MCP tools** — scene read and write tools for LLM agents
- **Scenario scoring** — JSON scenarios with roles and requirements (entity exists, on top of, between, near, no overlap, scale order, component value)
- **Model evaluation runner** — runs scenarios against Anthropic and OpenAI models concurrently, writes JSONL telemetry and the final scene JSON

## Requirements

- .NET 10+ SDK
- Docker + Docker Compose (API and Postgres)
- Anthropic and OpenAI API keys (Runner)

## Build & Run

### API

```bash
docker compose up --build
```

The API does not apply migrations on startup. With the database running, apply them from the host (needs `dotnet tool install --global dotnet-ef`):

```bash
dotnet ef database update --project Manipulator.Api \
  --connection "Host=localhost;Port=5432;Database=manipulator;Username=admin;Password=password"
```

Requests are authorized with a preconfigured API key sent as the `X-Api-Key` header (`dev-api-key` by default, override with `AUTHENTICATION_API_KEY`).

### Runner

```bash
dotnet user-secrets set Anthropic:ApiKey <key> --project Manipulator.Runner
dotnet user-secrets set OpenAI:ApiKey <key> --project Manipulator.Runner
dotnet run --project Manipulator.Runner
```

`appsettings.json` is only a template. Launch profiles pick a full configuration: **Scenario One** loads `appsettings.ScenarioOne.json`, **Scenario One Cheap** loads `appsettings.ScenarioOneCheap.json` (Claude Haiku and GPT-5 mini). Override any setting with environment variables or arguments, e.g. `--Runner:Batch=development`.

`Runner:Approach` picks the toolset; both go through the same MCP host, agent loop and system prompt. `mcp` (default) gives per-entity command tools plus `get_scene`, `get_entity` and `finish`. `text` gives only `get_scene`, `submit_scene` (replaces the whole scene, a rejected scene leaves it unchanged) and `finish`.

Output goes to `runs/<batch>/`:

| File                                            | Content                                       |
|-------------------------------------------------|-----------------------------------------------|
| `<model-name>.jsonl`                            | per-step telemetry, token usage, tool errors  |
| `<scenario>_<approach>_<model-name>_<index>.json` | final serialized scene                      |

## Tests

```bash
dotnet restore Manipulator.slnx
dotnet build Manipulator.slnx --configuration Release --no-restore
dotnet format Manipulator.slnx --verify-no-changes --no-restore
dotnet test Manipulator.slnx --configuration Release --no-build
```

## Project layout

| Project                | Role                                                       |
|------------------------|------------------------------------------------------------|
| `Manipulator.Core`     | ECS scene model, command and event pipeline                |
| `Manipulator.Api`      | REST API with PostgreSQL persistence                       |
| `Manipulator.Mcp`      | MCP tools over a scene                                     |
| `Manipulator.Scenarios`| scenario loading, validation and scoring                   |
| `Manipulator.Runner`   | model evaluation executable                                |
| `Manipulator.Examples` | small programs building scenes with the core library       |

Each project has a matching `.Tests` project.
