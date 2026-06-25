extends SceneTree


func _initialize() -> void:
	var scene := load("res://scenes/train_station_demo.tscn") as PackedScene
	var world := scene.instantiate()
	root.add_child(world)

	await physics_frame
	await physics_frame

	var player := world.get_node("Explorer") as CharacterBody3D
	var start_position := player.global_position
	var avatar_skeleton := player.get_node("Visual/Avatar/Skeleton3D") as Skeleton3D
	var hips_index := avatar_skeleton.find_bone("J_Bip_C_Hips")
	Input.action_press("ui_down")
	await physics_frame
	var initial_speed := Vector2(player.velocity.x, player.velocity.z).length()
	var min_hips_y := INF
	var max_hips_y := -INF
	for _frame in 59:
		await physics_frame
		var hips_y := avatar_skeleton.get_bone_pose_position(hips_index).y
		min_hips_y = min(min_hips_y, hips_y)
		max_hips_y = max(max_hips_y, hips_y)
	Input.action_release("ui_down")

	var running_speed := Vector2(player.velocity.x, player.velocity.z).length()
	assert(initial_speed < running_speed * 0.5, "Player acceleration is still effectively instant.")
	assert(max_hips_y - min_hips_y > 0.005, "Running animation has no visible pelvis bounce.")

	var moved_distance := Vector2(
		player.global_position.x - start_position.x,
		player.global_position.z - start_position.z
	).length()
	assert(moved_distance > 1.0, "Third-person player did not move.")
	assert(player.global_position.y > 0.5, "Player fell through the station platform.")

	var animation_player := player.find_child("AnimationPlayer", true, false) as AnimationPlayer
	assert(animation_player != null, "Imported character has no AnimationPlayer.")
	assert(
		animation_player.current_animation in ["Jog_Fwd", "Idle"],
		"Locomotion animation did not play."
	)

	for track_x in [-12.0, -8.1, -4.2]:
		player.global_position = Vector3(track_x, 1.5, 0.0)
		player.velocity = Vector3.ZERO
		for _frame in 15:
			await physics_frame
		assert(player.global_position.y > 0.8, "Player fell through track collision.")

	player.global_position.y = -10.0
	await physics_frame
	await physics_frame
	assert(player.global_position.y > 1.0, "Fall reset did not return the player to spawn.")

	print("THIRD_PERSON_RUNTIME_TEST_OK")
	quit()
