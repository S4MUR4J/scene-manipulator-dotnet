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

Runner reads defaults from `appsettings.json`, then lets user secrets,
environment variables, and command-line arguments override them. Set the
Anthropic API key outside tracked configuration:

```bash
dotnet user-secrets set ANTHROPIC_API_KEY <key> --project Manipulator.Runner
```

Run a scenario with:

```bash
dotnet run --project Manipulator.Runner -- \
  --scenario-file ../scenarios/s1-new-gen-livingroom.json \
  --scenario 1 \
  --batch adhoc
```

Useful options include `--variant`, `--model`, `--seed`, `--run-index`,
`--out-dir`, `--max-iterations`, and `--timeout-s`. Only the `mcp` approach is
implemented today.

## Outputs

By default, run artifacts are stored under `runs/<batch>/`:

- `<batch>.jsonl` contains per-step and completed-run telemetry, including
  model token usage and tool errors.
- `<scenario>_<approach>_<run-index>.json` contains the final serialized
  scene.
