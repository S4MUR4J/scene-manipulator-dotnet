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

## Configuration

Runner reads its complete research-run configuration from the checked-in
`appsettings.ScenarioOne.json` template. It does not accept environment
variables or command-line arguments for run configuration. Edit that template
to choose the scenario, limits, artifact location, and model matrix.

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

Run the scenario-one template with:

```bash
dotnet run --project Manipulator.Runner
```

Only the `mcp` approach is implemented today. Future scenario-specific
templates can use the same `Runner` configuration schema.

## Outputs

By default, run artifacts are stored under `runs/<batch>/`. Model names are
included in filenames so concurrently evaluated models never overwrite each
other:

- `<model-name>.jsonl` contains per-step and completed-run telemetry, including
  model token usage and tool errors.
- `<scenario>_<approach>_<model-name>_<run-index>.json` contains the final serialized
  scene.
