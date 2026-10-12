using System.Text.Json;
using FluentAssertions;
using Manipulator.Core.Ecs.Components;
using Manipulator.Runner.Configuration;
using Manipulator.Runner.Execution;
using Manipulator.Runner.Models;
using Manipulator.Runner.Tests.Helpers;

namespace Manipulator.Runner.Tests;

public class TextApproachTests
{
    private const string ValidScene = """
        {"entities": [
          {"id": "sofa", "components": {
            "transform": {"position": [0, 0.5, 0], "scale": [2, 1, 1]},
            "mesh_filter": {"geometry": "Cube"},
            "mesh_renderer": {"color": "#336699"},
            "entity_name": {"value": "sofa"}}},
          {"id": "floor", "components": {
            "mesh_filter": {"geometry": "Plane"},
            "entity_name": {"value": "floor"}}}
        ]}
        """;

    [Fact]
    public async Task SubmitScene_ValidScene_ReplacesSceneAndKeepsGivenIds()
    {
        await using var host = await RunningMcpHost.StartAsync(Approaches.Text);

        var executions = await ExecuteAsync(host, new RunMetrics(), SubmitScene(ValidScene));

        executions.Single().IsError.Should().BeFalse();
        host.Scene.Entities.Keys.Should().BeEquivalentTo("sofa", "floor");
        host.Scene.GetEntity("sofa")!.Get<Transform>()!.Scale.X.Should().Be(2);
    }

    [Fact]
    public async Task SubmitScene_Warnings_AreRecordedInMetricsAndStepLog()
    {
        await using var host = await RunningMcpHost.StartAsync(Approaches.Text);
        var metrics = new RunMetrics();
        const string scene = """
            {"entities": [{"id": "sofa", "components": {
              "mesh_filter": {"geometry": "Cube", "material": "velvet"},
              "rigid_body": {"mass": 3}}}]}
            """;

        var executions = await ExecuteAsync(host, metrics, SubmitScene(scene));

        metrics
            .ToolWarnings.Select(warning => warning.Message)
            .Should()
            .Contain(message => message.Contains("unknown field 'material'"))
            .And.Contain(message => message.Contains("unknown component type 'rigid_body'"))
            .And.Contain(message => message.Contains("missing component 'transform'"));
        metrics.ToolWarnings.Should().OnlyContain(warning => warning.Tool == "submit_scene");
        executions.Single().ToLog().Warnings.Should().HaveCount(metrics.ToolWarnings.Count);
    }

    [Fact]
    public async Task SubmitScene_Rejected_LeavesSceneUnchangedAndRecordsError()
    {
        await using var host = await RunningMcpHost.StartAsync(Approaches.Text);
        var metrics = new RunMetrics();
        await ExecuteAsync(host, metrics, SubmitScene(ValidScene));
        var versionBefore = host.Scene.Version;
        const string duplicateIds = """
            {"entities": [
              {"id": "a", "components": {"mesh_filter": {"geometry": "Cube"}}},
              {"id": "a", "components": {"mesh_filter": {"geometry": "Cube"}}}
            ]}
            """;

        var executions = await ExecuteAsync(host, metrics, SubmitScene(duplicateIds));

        executions.Single().IsError.Should().BeTrue();
        executions.Single().ErrorMessage.Should().Contain("Duplicate entity id 'a'");
        host.Scene.Entities.Keys.Should().BeEquivalentTo("sofa", "floor");
        host.Scene.Version.Should().Be(versionBefore);
        metrics.ToolErrors.Should().ContainSingle(error => error.Tool == "submit_scene");
    }

    [Fact]
    public async Task SubmitScene_NonPositiveScale_IsRejected()
    {
        await using var host = await RunningMcpHost.StartAsync(Approaches.Text);
        const string scene = """
            {"entities": [{"id": "a", "components": {
              "transform": {"scale": [1, 0, 1]},
              "mesh_filter": {"geometry": "Cube"}}}]}
            """;

        var executions = await ExecuteAsync(host, new RunMetrics(), SubmitScene(scene));

        executions.Single().ErrorMessage.Should().Contain("scale must be positive");
        host.Scene.Count.Should().Be(0);
    }

    [Fact]
    public async Task SubmitScene_SceneSentAsString_ReturnsSdkErrorToTheAgent()
    {
        await using var host = await RunningMcpHost.StartAsync(Approaches.Text);
        var call = new ModelToolCall(
            "call-1",
            "submit_scene",
            JsonDocument.Parse("""{"scene": "{\"entities\": []}"}""").RootElement.Clone()
        );

        var executions = await ExecuteAsync(host, new RunMetrics(), call);

        executions.Single().IsError.Should().BeTrue();
        executions.Single().ErrorMessage.Should().Contain("submit_scene");
        executions.Single().ResultText.Should().NotStartWith("Tool call failed");
    }

    [Fact]
    public async Task Metrics_CountEverySubmitAsWriteAndEveryGetSceneAsRead()
    {
        await using var host = await RunningMcpHost.StartAsync(Approaches.Text);
        var metrics = new RunMetrics();

        await ExecuteAsync(
            host,
            metrics,
            GetScene(),
            SubmitScene(ValidScene),
            SubmitScene("""{"entities": [{"id": "x", "components": {}}]}"""),
            GetScene()
        );

        metrics.SceneReads.Should().Be(2);
        metrics.SceneWrites.Should().Be(2);
        metrics.ToolErrors.Should().ContainSingle();
    }

    private static Task<List<ToolCallExecution>> ExecuteAsync(
        RunningMcpHost host,
        RunMetrics metrics,
        params ModelToolCall[] toolCalls
    ) => ToolCallExecutor.ExecuteAsync(host.Client, toolCalls, metrics, CancellationToken.None);

    private static ModelToolCall SubmitScene(string sceneJson) =>
        new ModelToolCall(
            Guid.NewGuid().ToString("n"),
            "submit_scene",
            JsonDocument.Parse($$"""{"scene": {{sceneJson}}}""").RootElement.Clone()
        );

    private static ModelToolCall GetScene() =>
        new ModelToolCall(
            Guid.NewGuid().ToString("n"),
            "get_scene",
            JsonDocument.Parse("{}").RootElement.Clone()
        );
}
