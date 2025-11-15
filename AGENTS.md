# Repository Guidelines

## Project Structure & Module Organization
`NodeGraph.slnx` keeps source under `src/` and specs under `test/`. `src/NodeGraph.Model` houses the runtime (nodes, ports, pools, serialization) consumed by the editor and generator. `src/NodeGraph.Editor` is the Avalonia MVVM client; organize updates within `ViewModels`, `Views`, `Services`, `Selection`, and `Undo`. `src/NodeGraph.Generator` hosts the Roslyn generator (`SourceGenerator.cs`, `CSharpCodeGenerator.cs`). Tests live in `test/NodeGraph.UnitTest`, mirroring model namespaces with `*Test.cs` files, including the `Serialization/` folder.

## Build, Test, and Development Commands
Run `dotnet restore NodeGraph.slnx` before development. `dotnet build NodeGraph.slnx -c Release` compiles the generator, model, and editor together. Launch the UI with `dotnet run --project src/NodeGraph.Editor/NodeGraph.Editor.csproj` to verify bindings. Execute tests via `dotnet test test/NodeGraph.UnitTest/NodeGraph.UnitTest.csproj --configuration Release`, adding `--collect:"XPlat Code Coverage"` only when measuring coverage.

## Coding Style & Naming Conventions
Use file-scoped namespaces with `using` statements outside namespace blocks, 4-space indentation, and `var` when the type is obvious. Favor expression-bodied members and target-typed `new()` as shown in `src/NodeGraph.Model/Graph.cs`. Keep types and public members in `PascalCase`, locals in `camelCase`, prefix interfaces with `I`, and reuse suffixes (`*Node`, `*Port`, `*Pool`). Prefer immutable fields, initialize collections with `[]`, and run `dotnet format` before opening a PR.

## Testing Guidelines
xUnit drives the suite (`[Fact]`, `[Theory]`) with Arrange/Act/Assert blocks as in `test/NodeGraph.UnitTest/GraphCloneTest.cs`. Name methods `<Member>_<Scenario>_<Expectation>`, keep async tests returning `Task`, and mimic production behaviors such as renting pools before asserting results. Cover model and generator edge cases ahead of UI assertions, and accompany every bug fix with a regression test plus any required fixtures.

## Commit & Pull Request Guidelines
History favors short, imperative commit subjects, often in Japanese (e.g., “ノード整列処理を改良し...”); keep that tone and limit each commit to one logical change. Reference issues in the body, call out risk areas, and list the validation you ran (`dotnet test`, editor smoke run). Pull requests should summarize the change, include reproduction or verification steps, attach UI evidence when applicable, and mention migrations or config updates. Request reviews from the owning area (Model, Editor, Generator) and wait for all automated runs to pass before merging.
