# Time Loop Godot Demo

A minimal Godot 4 C# 2D game demo about a disordered time loop, intervention, regret, and acceptance.

## Project overview

The demo is built as a Godot 4 C# 2D top-down prototype based on `../design.md`:

- `scenes/Main.tscn` contains the full gameplay tree: `Root`, `TimeStateManager`, `MainStage`, `Dynamic_Props`, `Light_System`, `Player_Character`, and `UILayer`.
- `scripts/GameController.cs` coordinates interaction prompts, inspection overlay, calendar matrix, glitch/whiteout transitions, and room blockout visuals.
- `scripts/TimeStateManager.cs` manages hidden time states: Day -3, Day 0, and Day +7.
- `scripts/TemporalProp.cs` powers date-dependent inspectable props such as the diary, recorder, pill bottle, and calendar.
- `scripts/PlayerCharacter.cs` handles top-down movement and hooks for Idle/Walk/Interact/Breakdown animations.

The story flow remains the same as the original prototype. See `RESOURCE_PLACEMENT.md` for the exact folders and scene nodes where art, transition animation, shader, and audio resources should be placed.

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

## Publishing to GitHub

This project is already initialized as a local Git repository on the `main` branch.

To publish it to GitHub:

1. Create an empty GitHub repository in your GitHub account or organization.
2. Copy the repository URL.
3. Add that URL as the `origin` remote.
4. Push the local `main` branch.

Avoid committing Godot editor caches or build outputs. The included `.gitignore` is designed to keep those generated files out of the repository.

## Current playable flow

1. Opening apartment scene
2. Accident-night subway platform scene
3. Apartment before the accident
4. Apartment after the accident
5. Final platform choice
6. Acceptance ending
