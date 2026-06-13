using Manipulator.Core.Commands;
using Manipulator.Core.Commands.Handlers;
using Manipulator.Core.Commands.Validation;
using Manipulator.Core.Ecs;
using Manipulator.Core.Events;
using Manipulator.Core.IdGeneration;
using Manipulator.Core.Serialization;

namespace Manipulator.Examples.Programs;

public static class BasicScene
{
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

        // Add a cube.
        Console.WriteLine("Adding a cube...");
        var addCubeResult = commandDispatcher.Dispatch(
            command: new AddEntityCommand(
                Geometry: GeometryType.Cube,
                Position: Vector3.Forward,
                Name: "Block"
            )
        );
        if (!addCubeResult.IsSuccess)
        {
            Console.WriteLine($"Error: {addCubeResult.Error}");
            return;
        }
        Console.WriteLine($"Cube add data: {addCubeResult.Data}.");
        Console.WriteLine();

        // Add a sphere.
        Console.WriteLine("Adding a sphere...");
        var addSphereResult = commandDispatcher.Dispatch(
            command: new AddEntityCommand(
                Geometry: GeometryType.Sphere,
                Position: Vector3.Right,
                Name: "Earth"
            )
        );
        if (!addSphereResult.IsSuccess)
        {
            Console.WriteLine($"Error: {addSphereResult.Error}");
            return;
        }
        Console.WriteLine($"Sphere add data: {addSphereResult.Data}.");
        Console.WriteLine();

        // Add a triangle.
        Console.WriteLine("Adding a triangle...");
        var addTriangleResult = commandDispatcher.Dispatch(
            command: new AddEntityCommand(
                Geometry: GeometryType.Pyramid,
                Position: Vector3.Up,
                Name: "Triangle"
            )
        );
        if (!addTriangleResult.IsSuccess)
        {
            Console.WriteLine($"Error: {addTriangleResult.Error}");
            return;
        }
        Console.WriteLine($"Triangle add data: {addTriangleResult.Data}.");
        Console.WriteLine();

        Console.WriteLine($"Number of element on scene: {scene.Entities.Count}");
        Console.WriteLine();

        // Serialize and print scene as JSON.
        Console.WriteLine("Serializing scene...");
        var json = SceneSerializer.Serialize(scene);
        Console.WriteLine("Scene serialized as JSON:");
        Console.WriteLine();
        Console.WriteLine(json);
    }
}
