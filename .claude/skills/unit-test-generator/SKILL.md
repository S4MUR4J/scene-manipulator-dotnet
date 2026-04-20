---
name: unit-test-generator
description: Generate xUnit tests for a C# class in this project. Use when user says "napisz testy", "wygeneruj testy", "dodaj testy", "generate tests", "write tests", or invokes /unit-test-generator.
argument-hint: <ClassName or file path>
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Write, Bash
---

Generate xUnit tests for: $ARGUMENTS

## Stack

- xUnit 2.x — `[Fact]`, `[Theory]`, `[InlineData]`
- FluentAssertions — `result.Should().Be(...)`, `act.Should().Throw<...>()`
- `Xunit` is globally imported (no `using Xunit;` needed)
- Target framework: net10.0, nullable enabled

## Steps

1. Read the source file for $ARGUMENTS
2. Read existing tests in `Manipulator.Core.Tests/` for style reference — see [examples/EntityTests.cs](examples/EntityTests.cs)
3. Identify all public methods/properties worth testing
4. Determine correct test file path:
   - Source `Manipulator.Core/Ecs/Foo.cs` → test `Manipulator.Core.Tests/Ecs/FooTests.cs`
   - Match namespace: `Manipulator.Core.Tests.<subfolder>`
5. **Present the proposed test case list** in this format and STOP — wait for user approval before writing any code:
   ```
   Proposed test cases for FooTests:
   - MethodName_Condition_ExpectedResult
   - MethodName_Condition_ExpectedResult
   ...
   Approve or adjust the list.
   ```
6. After user approves (or provides corrections/additions), write all tests at once
7. Run `dotnet test` and fix any failures

## Conventions

- One `public class FooTests` per file
- Test method names: `MethodName_Condition_ExpectedResult`
- **AAA pattern** — every test body has exactly three comment sections: `// Arrange`, `// Act`, `// Assert`; collapse Arrange+Act into `// Arrange & Act` only when there is no meaningful arrange step (e.g. constructor-only tests)
- No mocks — test against real objects
- Mutations via `internal` methods require same assembly; `[assembly: InternalsVisibleTo]` is already set
- Group related tests with `#region` only if file exceeds ~80 lines
- **Never use `// Act & Assert`** — always keep Act and Assert as separate sections
- **Property/getter tests must have an explicit `// Act` step**: `var result = sut.Property;` then assert on `result`, never inline the access in the assert
- **Class name must exactly match file name**: `FooTests.cs` → `public class FooTests`
- **Vector3 literals** — use direction constants (`Vector3.Right`, `Vector3.Up`, `Vector3.One`, etc.) and scalar multiplication (`Vector3.Right * 2f`) instead of raw `new Vector3(x, y, z)` whenever the value matches a constant; keep raw literals only for unique values like `new Vector3(1, 2, 3)`
- **Use `.Be()` for reference identity** (same object returned from a store/collection); use `.BeEquivalentTo()` only when comparing structurally distinct instances intentionally

## Builders (use these — never create entities/scenes manually)

Helpers live in `Manipulator.Core.Tests.Helpers`:

```csharp
// Entity with no components
var entity = new EntityBuilder(SceneBuilder.Id(1)).Build();

// Entity with components
var entity = new EntityBuilder(SceneBuilder.Id(1))
    .WithComponent(new Transform { Position = Vector3.Right })
    .WithComponent(new EntityName("Player"))
    .Build();

// Empty scene
var scene = new SceneBuilder().Build();

// Scene with one auto-tagged entity ("entity_1")
var scene = new SceneBuilder().WithEntity().Build();

// Scene with one named entity
var scene = new SceneBuilder().WithEntity("player").Build();

// Scene with one configured entity
var scene = new SceneBuilder()
    .WithEntity(e => e.WithComponent(new Transform()))
    .Build();

// Scene with N auto-tagged entities ("entity_1" … "entity_N")
var scene = new SceneBuilder().WithEntities(3).Build();

// Scene with N custom-tagged entities ("obstacle_1" … "obstacle_N")
var scene = new SceneBuilder().WithEntities(3, "obstacle").Build();
```

**ID references** — use `SceneBuilder.Id(n)` to refer to auto-tagged entities by index:

```csharp
scene.HasEntity(SceneBuilder.Id(1)).Should().BeTrue();
scene.RemoveEntity(SceneBuilder.Id(2));
```

For non-existing IDs use a high index unlikely to collide: `SceneBuilder.Id(99)`.

**Variable names:** `firstEntity`/`secondEntity` — never bare `first`/`second`.