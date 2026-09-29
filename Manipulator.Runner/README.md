# Manipulator.Runner

`Manipulator.Runner` is the research and evaluation executable for
scene-manipulation agents. It runs a scenario against a configured AI model,
provides the model with MCP scene tools, and records what happened.

For every run, Runner:

- loads a scenario and its starting scene;
- starts an isolated in-process MCP host using `Manipulator.Mcp`;
- sends the scenario prompt and discovered MCP tools to the configured model;
- enforces iteration and timeout limits; and
- writes JSONL telemetry and the final scene JSON to the configured output
  directory.

It is not a persistent or deployed MCP service. A future `Manipulator.Server`
will provide the independently hosted API and MCP surface.

## Project layout

- `Configuration/` contains settings binding and validated run configuration.
- `Execution/` contains process hosting, the application coordinator, and the
  model/tool execution loop.
- `Mcp/` contains the isolated per-run MCP host and its completion tool.
- `Models/` contains model-provider strategies and their shared abstractions.
- `Logging/` contains Serilog-backed, per-run artifact output and run-record
  serialization.

## Configuration

`appsettings.json` is a commented configuration template and intentionally
does not define a runnable configuration. Launch profiles select an
`appsettings.{DOTNET_ENVIRONMENT}.json` file with the complete settings: the
**Manipulator.Runner - Scenario One** profile loads
`appsettings.ScenarioOne.json`. The **Manipulator.Runner - Scenario One Cheap**
profile loads `appsettings.ScenarioOneCheap.json`, which uses Claude Haiku
(`claude-haiku-4-5`) and GPT-5 mini (`gpt-5-mini`) to reduce model costs.

Configuration sources are applied in this order, with later sources overriding
earlier values:

1. `appsettings.json`
2. `appsettings.{DOTNET_ENVIRONMENT}.json`
3. Environment variables
4. Command-line arguments
5. User secrets

Use environment variables or command-line arguments for local overrides, for
example `Runner__Batch=development` or `--Runner:Batch=development`.

The only settings stored outside tracked configuration are provider API keys.
Set both keys when using the default Anthropic/OpenAI matrix:

```bash
dotnet user-secrets set Anthropic:ApiKey <key> --project Manipulator.Runner
dotnet user-secrets set OpenAI:ApiKey <key> --project Manipulator.Runner
```

Each enabled `Runner:Models` entry has a stable name, provider, and provider
model ID. A single invocation runs all enabled entries concurrently. Runner
validates every enabled provider key before it starts any model, and fails the
whole matrix if one is missing.

Run the base configuration with:

```bash
dotnet run --project Manipulator.Runner
```

Set `Runner:Approach` (for example `--Runner:Approach=text`) to pick which
tools the model gets. Both approaches run through the same MCP host, agent loop
and system prompt, so their results stay comparable:

- `mcp` (default): per-entity command tools (`add_entity`, `move_entity`, ...)
  plus `get_scene`, `get_entity` and `finish`.
- `text`: a declarative, LayoutGPT-style approach with exactly `get_scene`,
  `submit_scene` and `finish`. `submit_scene` takes a whole scene as a JSON
  object and replaces the current one. Entity ids are kept as given and
  duplicates are rejected. The scene must also pass the rules `add_entity`
  applies: every entity needs a `mesh_filter`, and scale must be positive. A
  rejected submission leaves the scene unchanged and returns the error so the
  model can retry. Unknown components or fields, and missing components that
  get filled with defaults, are reported back as warnings.

`dsl` is not implemented yet.

## Outputs

By default, run artifacts are stored under `runs/<batch>/`. Model names are
included in filenames so concurrently evaluated models never overwrite each
other:

- `<model-name>.jsonl` contains per-step and completed-run telemetry, including
  model token usage, tool errors (`ToolErrors`, which include rejected
  `submit_scene` calls) and tool warnings (`ToolWarnings`). Scene reads count
  `get_scene`/`get_entity` calls. Scene writes count every mutating tool call,
  including each `submit_scene` attempt, whether or not it was accepted.
- `<scenario>_<approach>_<model-name>_<run-index>.json` contains the final serialized
  scene.

Runner lifecycle diagnostics are emitted through the host's Serilog pipeline.
Research artifacts remain isolated per model run, so concurrent evaluations
produce deterministic JSONL and final-scene paths.
