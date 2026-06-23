# Time Loop Godot Demo

A minimal Godot 4 C# 3D game demo about a disordered time loop, intervention, regret, and acceptance.

## Project overview

The demo is built with Godot's scene tree plus C# runtime scene generation. The main scene is intentionally lightweight:

- `scenes/Main.tscn` contains the root node.
- `scripts/GameController.cs` procedurally creates the apartment and subway platform scenes at runtime.
- `scripts/TimeManager.cs` is registered as an Autoload singleton and stores the current time phase, loop count, memory log, and cross-time object states.

## Requirements

- Godot 4.7 Mono or compatible Godot 4.x .NET build
- .NET SDK 8.0+
- Git

## Getting started

1. Clone the repository.
2. Open the `game-demo` folder with Godot Mono.
3. Let Godot restore/build the C# project when prompted.
4. Run `scenes/Main.tscn`.

## Build from terminal

From the project root:

`dotnet build .\game_demo.csproj`

If you use a custom local .NET SDK installation, make sure `DOTNET_ROOT` and `PATH` point to that SDK before building.

## Version control notes

This repository tracks source files and Godot metadata that are needed for collaborators, including:

- `.tscn` scenes
- `.cs` scripts
- `.csproj` / `.sln`
- `project.godot`
- `.import` and `.uid` metadata files

Generated editor caches and build outputs are ignored, including:

- `.godot/`
- `.mono/`
- `bin/`
- `obj/`
- export builds such as `.apk`, `.aab`, `.pck`, `.exe`, `.zip`

## Current playable flow

1. Opening apartment scene
2. Accident-night subway platform scene
3. Apartment before the accident
4. Apartment after the accident
5. Final platform choice
6. Acceptance ending
