extends CharacterBody3D

@export var walk_speed := 3.5
@export var run_speed := 6.5
@export var jump_velocity := 4.3
@export var mouse_sensitivity := 0.0022
@export var fly_speed := 7.0
@export var fly_fast_speed := 16.0

@onready var head: Node3D = $Head
@onready var camera: Camera3D = $Head/Camera3D

var _pitch := 0.0
var _mouse_captured := true
var _fly_mode := false
var _gravity: float = ProjectSettings.get_setting("physics/3d/default_gravity")


func _ready() -> void:
	camera.current = true
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	_update_mode_label()


func _exit_tree() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and _mouse_captured:
		rotate_y(-event.relative.x * mouse_sensitivity)
		_pitch = clamp(
			_pitch - event.relative.y * mouse_sensitivity,
			deg_to_rad(-88.0),
			deg_to_rad(88.0)
		)
		head.rotation.x = _pitch

	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT and not _mouse_captured:
			_set_mouse_capture(true)

	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_ESCAPE:
			_set_mouse_capture(not _mouse_captured)
		elif event.keycode == KEY_F11:
			_toggle_fullscreen()
		elif event.keycode == KEY_F:
			_fly_mode = not _fly_mode
			velocity = Vector3.ZERO
			_update_mode_label()


func _physics_process(delta: float) -> void:
	var input_2d := Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	input_2d.x += float(Input.is_physical_key_pressed(KEY_D)) - float(Input.is_physical_key_pressed(KEY_A))
	input_2d.y += float(Input.is_physical_key_pressed(KEY_S)) - float(Input.is_physical_key_pressed(KEY_W))
	input_2d = input_2d.limit_length(1.0)

	var direction := (transform.basis * Vector3(input_2d.x, 0.0, input_2d.y)).normalized()
	if _fly_mode:
		_process_flying(direction)
	else:
		_process_walking(direction, delta)


func _process_walking(direction: Vector3, delta: float) -> void:
	var speed := run_speed if Input.is_key_pressed(KEY_SHIFT) else walk_speed
	velocity.x = direction.x * speed
	velocity.z = direction.z * speed

	if not is_on_floor():
		velocity.y -= _gravity * delta
	elif Input.is_key_pressed(KEY_SPACE):
		velocity.y = jump_velocity

	move_and_slide()


func _process_flying(direction: Vector3) -> void:
	var vertical := 0.0
	if Input.is_key_pressed(KEY_SPACE) or Input.is_key_pressed(KEY_E):
		vertical += 1.0
	if Input.is_key_pressed(KEY_Q) or Input.is_key_pressed(KEY_CTRL):
		vertical -= 1.0

	var speed := fly_fast_speed if Input.is_key_pressed(KEY_SHIFT) else fly_speed
	var motion := direction + Vector3.UP * vertical
	velocity = motion.normalized() * speed if motion.length_squared() > 0.0 else Vector3.ZERO
	move_and_slide()


func _set_mouse_capture(captured: bool) -> void:
	_mouse_captured = captured
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED if captured else Input.MOUSE_MODE_VISIBLE


func _update_mode_label() -> void:
	var label := get_node_or_null("../Interface/ModeLabel") as Label
	if label:
		label.text = "MODE: FLY" if _fly_mode else "MODE: WALK"


func _toggle_fullscreen() -> void:
	if DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
