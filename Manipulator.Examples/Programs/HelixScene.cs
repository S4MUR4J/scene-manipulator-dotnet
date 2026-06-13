using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Commands.Validation;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Core.IdGeneration;
using Manipulator.Core.Serialization;

namespace Manipulator.Examples.Programs;

public static class HelixScene
{
    private const int Parts = 60;
    private const float Radius = 2.0f;
    private const float HeightStep = 0.5f;
    private const float TurnsTotal = 2.0f;
    private const int DelayMs = 50;

    public static void Run()
    {
        // Set up a scene system.
        var scene = new Scene();
        var eventBus = new EventBus();
        var commandDispatcher = new CommandDispatcher(scene: scene, eventBus: eventBus);

        // Register AddEntity handler.
        commandDispatcher.Register(
            commandType: "AddEntity",
            handler: new AddEntityHandler(new GuidGenerator()),
            validators: new VersionConflictValidator()
        );

        // Configure the output path where the scene JSON will be saved after each step.
        var outputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".data",
            "scene.json"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        // Build helix loop: one block for left and right stand per iteration
        for (var step = 0; step < Parts; step++)
        {
            var angle = (float)step / Parts * TurnsTotal * 2 * MathF.PI;
            var height = step * HeightStep;

            var leftPosition = new Vector3(
                Radius * MathF.Cos(angle),
                height,
                Radius * MathF.Sin(angle)
            );

            var rightPosition = new Vector3(
                Radius * MathF.Cos(angle + MathF.PI),
                height,
                Radius * MathF.Sin(angle + MathF.PI)
            );

            // Add left strand block.
            Console.WriteLine($"Step {step + 1}/{Parts} — adding left block...");
            var leftResult = commandDispatcher.Dispatch(
                new AddEntityCommand(
                    Geometry: GeometryType.Cube,
                    Position: leftPosition,
                    Name: $"Left_{step}"
                )
            );
            if (!leftResult.IsSuccess)
            {
                Console.WriteLine($"Error: {leftResult.Error}");
                return;
            }

            // Add right strand block.
            Console.WriteLine($"Step {step + 1}/{Parts} — adding right block...");
            var rightResult = commandDispatcher.Dispatch(
                new AddEntityCommand(
                    Geometry: GeometryType.Cube,
                    Position: rightPosition,
                    Name: $"Right_{step}"
                )
            );
            if (!rightResult.IsSuccess)
            {
                Console.WriteLine($"Error: {rightResult.Error}");
                return;
            }

            // Serialize the current scene state and save it to disk.
            var json = SceneSerializer.Serialize(scene);
            File.WriteAllText(outputPath, json);
            Console.WriteLine($"Step {step + 1}/{Parts} — scene saved to {outputPath}");
            Console.WriteLine();

            // Wait before adding the next pair of blocks.
            Thread.Sleep(DelayMs);
        }

        Console.WriteLine("Helix complete.");
    }
}
