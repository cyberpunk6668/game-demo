# Isometric Apartment Room

This folder is an additive 3D room demo. It does not replace or modify the
existing game main scene or C# gameplay code.

## Open the room

1. Open the repository in Godot 4.7 Mono.
2. Open `room_demo/scenes/isometric_apartment_demo.tscn`.
3. Run the current scene.

## Controls

- `WASD`: move
- Mouse: look
- `Space`: jump
- `Shift`: run
- `F`: switch between walking and flying
- Flying: `Space/E` up, `Q/Ctrl` down
- `F11`: toggle fullscreen
- `Esc`: release the mouse

## Source assets

- `assets/isometric_apartment.glb`: Godot-ready room model with embedded textures.
- `assets/source/isometric_apartment.blend`: editable Blender source.
- `assets/isometric_apartment_*_ai.png`: painterly textures extracted for Godot.
- `assets/isometric_apartment_preview.png`: preview render.

The Blender source has all eight painterly textures packed into the `.blend`
file, so it can be opened without restoring external texture paths.
