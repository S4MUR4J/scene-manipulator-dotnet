# scene-manipulator-dotnet

ECS-based scene data library with a unified command interface for manipulating 3D
objects — configurable to work with LLMs, keyboard input, GUIs, or any other
controller. The repo is a REST API (`Manipulator.Api`) backed by PostgreSQL for
scene persistence, a pure C# ECS core (`Manipulator.Core`) that owns the scene
model and command/event pipeline, and a research runner (`Manipulator.Runner`)
that evaluates AI agents against isolated in-memory MCP scenes using
`Manipulator.Mcp`. See [docs/architecture.md](docs/architecture.md) for the
full component breakdown and diagrams.

> Under active development.

## Requirements

- .NET 10+ SDK
- Docker + Docker Compose (for running the API and Postgres locally)

## Run locally

`docker compose up --build` starts `Manipulator.Api` and a Postgres database.

The API refuses to start without an `Authentication:ApiKey` value — every request
must send it back as the `X-Api-Key` header (see [docs/api.md](docs/api.md)).
Set it via the `AUTHENTICATION_API_KEY` environment variable before starting
Compose, e.g. in a `.env` file next to `compose.yaml`:

```
AUTHENTICATION_API_KEY=dev-api-key
```

If unset, Compose falls back to `dev-api-key` for local convenience — do not rely
on that default outside local development.

```bash
docker compose up --build
```

- `Manipulator.Api` → http://localhost:8000

On first run, apply the EF Core migrations against the Postgres container (the
API does not apply them automatically):

```bash
dotnet tool install --global dotnet-ef   # if not already installed
dotnet ef database update --project Manipulator.Api \
  --connection "Host=localhost;Port=5432;Database=manipulator;Username=admin;Password=password"
```

Verify the API is up:

```bash
curl -H "X-Api-Key: dev-api-key" http://localhost:8000/api/v1/scenes/
```

## Tests and format check

These are the same commands CI runs (`.github/workflows/ci.yml`):

```bash
dotnet restore Manipulator.slnx
dotnet build Manipulator.slnx --configuration Release --no-restore
dotnet format Manipulator.slnx --verify-no-changes --no-restore
dotnet test Manipulator.slnx --configuration Release --no-build
```

## Running research scenarios

`Manipulator.Runner` evaluates configured AI models against scene-manipulation
scenarios. It starts an isolated, in-process MCP host for each run, records
tool calls and model metrics, and saves the resulting scene. See
[Manipulator.Runner/README.md](Manipulator.Runner/README.md) for setup and
usage.

## Docs

- [docs/architecture.md](docs/architecture.md) — system overview and diagrams
- [docs/api.md](docs/api.md) — endpoints, auth, versioning, scene content JSON contract
- [docs/adr/](docs/adr/) — architectural decision records
