# Character turn and movement animation plan

The current project now supports smooth procedural turning in `scripts/WalkExplorer.cs`.

The original GLB character parts named `Character_*` are combined under:

`Explorer/OriginalHumanModel`

WASD moves `Explorer`, so the original human model moves with the collision body.

## Current procedural animation already implemented

No extra art is required for this baseline:

- Smooth yaw turn toward movement direction.
- Slight turn lean on the visible human model.
- Subtle walk bob while moving.

Tunable values on `Explorer`:

- `TurnSpeedDegrees`
- `TurnLeanDegrees`
- `WalkBobAmplitude`
- `WalkBobSpeed`

## Recommended animation clips to prepare later

If you want high-quality character animation, prepare these clips for the original human model or a future rigged replacement:

1. `Idle`
   - Slow breathing and subtle body sway.
   - Used when no movement input is pressed.

2. `WalkForward`
   - Normal walking cycle.
   - Should loop cleanly.

3. `RunForward`
   - Faster movement cycle for Shift run.
   - Should loop cleanly.

4. `TurnLeft90`
   - A short 90-degree left turn in place.
   - Useful when player changes direction sharply.

5. `TurnRight90`
   - A short 90-degree right turn in place.

6. `TurnAround180`
   - A 180-degree turn.
   - Useful when pressing the opposite direction.

7. `StartWalk`
   - Weight-shift transition from idle into walk.

8. `StopWalk`
   - Transition from walk back to idle.

9. `Interact`
   - Lean forward / reach hand out.
   - Useful for inspecting props later.

## Suggested resource folders

Put future animation/model resources here:

- `assets/characters/player/model/`
- `assets/characters/player/animations/idle/`
- `assets/characters/player/animations/walk/`
- `assets/characters/player/animations/run/`
- `assets/characters/player/animations/turn_left/`
- `assets/characters/player/animations/turn_right/`
- `assets/characters/player/animations/turn_around/`
- `assets/characters/player/animations/interact/`

## Important rigging note

The current GLB character is split into separate mesh parts:

- `Character_Torso`
- `Character_Head`
- `Character_Arm_-1`
- `Character_Arm_1`
- `Character_Leg_-1`
- `Character_Leg_1`
- `Character_Shoe_-1`
- `Character_Shoe_1`

For real skeletal animation, export a rigged character as `.glb` with a `Skeleton3D` and `AnimationPlayer`, then replace or nest it under `Explorer`.
