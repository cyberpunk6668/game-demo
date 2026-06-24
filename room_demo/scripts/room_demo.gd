extends Node3D


func _ready() -> void:
	var imported_camera := find_child("IsometricCamera", true, false) as Camera3D
	if imported_camera:
		imported_camera.current = false
