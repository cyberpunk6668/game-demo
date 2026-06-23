# Resource placement guide

Use these folders for the final art, animation, shader, and audio resources. The current project already has matching scene nodes in `scenes/Main.tscn`.

## Player animation resources

- `assets/characters/player/spritesheets/`
  - Put the 4-direction player SpriteSheet textures here.
  - Scene node: `Root/MainStage/Player_Character/Sprite2D`

- `assets/characters/player/animations/idle/`
  - Idle breathing/trembling frames or animation data.
  - Scene node: `Root/MainStage/Player_Character/AnimationPlayer`
  - Animation name to create: `Idle`

- `assets/characters/player/animations/walk/`
  - Heavy walk cycle and afterimage resources.
  - Scene node: `Root/MainStage/Player_Character/AnimationPlayer`
  - Animation name to create: `Walk`

- `assets/characters/player/animations/interact/`
  - Forward-leaning inspect animation.
  - Scene node: `Root/MainStage/Player_Character/AnimationPlayer`
  - Animation name to create: `Interact`

- `assets/characters/player/animations/breakdown/`
  - Kneeling/trembling breakdown frames.
  - Scene node: `Root/MainStage/Player_Character/AnimationPlayer`
  - Animation name to create: `Breakdown`

## Prop resources

- `assets/props/diary/`
  - Diary sprites for Day -3, Day 0, Day +7.
  - Scene node: `Root/MainStage/Dynamic_Props/Desk_Diary/Sprite2D`

- `assets/props/audio_recorder/`
  - Recorder sprites and tape state variants.
  - Scene node: `Root/MainStage/Dynamic_Props/Audio_Recorder/Sprite2D`

- `assets/props/pill_bottle/`
  - Pill bottle variants for the three time states.
  - Scene node: `Root/MainStage/Dynamic_Props/Pill_Bottle/Sprite2D`

- `assets/props/calendar/`
  - Old calendar art and marker-circle UI art.
  - Scene nodes: `Root/MainStage/Dynamic_Props/Calendar_Anchor/Sprite2D`, `Root/UILayer/CalendarMatrix`

## Transition animation resources

- `assets/transitions/sustained_jump_glitch/`
  - Full-screen glitch animation textures, shader, or noise masks.
  - Scene node: `Root/UILayer/GlitchShaderRect`
  - Controller method: `TriggerSustainedJumpGlitch()`

- `assets/transitions/whiteout_collapse/`
  - White-out collapse animation, white flash masks, and fade curves.
  - Scene node: `Root/UILayer/GlitchShaderRect`
  - Controller method: `TriggerWhiteoutCollapse()`

- `assets/transitions/vignette_inspection/`
  - Inspection vignette shader or overlay texture.
  - Scene node: `Root/UILayer/InspectionOverlay`
  - Controller method: `FadeInspectionOverlay()`

- `assets/transitions/retrograde_rain/`
  - Reverse rain particle texture and particle material resources.
  - Scene node: `Root/MainStage/VFX_System/Window_Rain_Retrograde`

- `assets/transitions/normal_rain/`
  - Normal rain particle texture and particle material resources.
  - Scene node: `Root/MainStage/VFX_System/Window_Rain_Normal`

## Shader resources

- `assets/shaders/glitch/`
  - Put the full-screen glitch shader here.
  - Attach it as a `ShaderMaterial` to `Root/UILayer/GlitchShaderRect`.
  - Expected shader parameter: `glitch_intensity`.

- `assets/shaders/vignette/`
  - Put the inspection vignette shader here.
  - Attach it as a `ShaderMaterial` to `Root/UILayer/InspectionOverlay`.

## Audio resources

- `assets/audio/recorder/day_minus3/`
  - Female protagonist's cheerful recorder message.
  - Logical prop: `Audio_Recorder` Day -3.

- `assets/audio/recorder/day_zero/`
  - Tape blind noise/static.
  - Logical prop: `Audio_Recorder` Day 0.

- `assets/audio/recorder/day_plus7/`
  - Police message recording.
  - Logical prop: `Audio_Recorder` Day +7.

- `assets/audio/transitions/glitch/`
  - Ear-ringing, scream, and red-glitch transition sounds.
  - Scene node: `Root/UILayer/GlitchAudioPlayer`.

- `assets/audio/transitions/whiteout/`
  - White-out collapse and final calm fade sounds.
  - Scene node: `Root/UILayer/GlitchAudioPlayer` or `Root/UILayer/RainAudioPlayer`.

- `assets/audio/ambient/rain/`
  - Rain loop and reversed rain variants.
  - Scene node: `Root/UILayer/RainAudioPlayer`.

- `assets/audio/ambient/train/`
  - Train headlight/rail ambience and approaching train rumble.
  - Scene node: `Root/UILayer/TrainAudioPlayer`.
