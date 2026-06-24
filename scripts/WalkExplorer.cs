using Godot;

public partial class WalkExplorer : CharacterBody3D
{
	[Export] public float WalkSpeed { get; set; } = 3.5f;
	[Export] public float RunSpeed { get; set; } = 6.5f;
	[Export] public float JumpVelocity { get; set; } = 4.3f;
	[Export] public float FlySpeed { get; set; } = 7.0f;
	[Export] public float FlyFastSpeed { get; set; } = 16.0f;
	[Export] public bool UseSceneCameraTransform { get; set; } = true;
	[Export] public bool FixedRoomCamera { get; set; } = true;
	[Export] public Vector3 RoomViewCenter { get; set; } = new(0.9f, 0.75f, -0.45f);
	[Export] public Vector3 CameraOffset { get; set; } = new(0.0f, 8.2f, 5.3f);
	[Export] public Vector3 CameraLookAtOffset { get; set; } = new(0.0f, 0.85f, -0.25f);
	[Export] public float CameraFollowSpeed { get; set; } = 8.0f;
	[Export] public float OrthographicSize { get; set; } = 7.8f;
	[Export] public bool GenerateHumanModelIfMissing { get; set; } = true;
	[Export] public bool HidePlaceholderBodyVisual { get; set; } = true;

	private Node3D _head = null!;
	private Camera3D _camera = null!;
	private Transform3D _sceneCameraTransform;
	private bool _flyMode;
	private float _gravity;

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity").AsDouble();
		EnsureMovableHumanModel();

		_camera.Current = true;
		_sceneCameraTransform = _camera.GlobalTransform;
		_camera.TopLevel = true;

		if (UseSceneCameraTransform)
		{
			_camera.GlobalTransform = _sceneCameraTransform;
		}
		else
		{
			_camera.Set("projection", 1);
			_camera.Set("size", OrthographicSize);
			UpdateCameraImmediate();
		}

		Input.MouseMode = Input.MouseModeEnum.Visible;
		UpdateModeLabel();
	}

	private void EnsureMovableHumanModel()
	{
		if (HidePlaceholderBodyVisual && GetNodeOrNull<Node3D>("BodyVisual") is Node3D placeholderBody)
		{
			placeholderBody.Visible = false;
		}

		if (!GenerateHumanModelIfMissing || GetNodeOrNull<Node3D>("PlayerHumanModel") != null)
		{
			return;
		}

		var modelRoot = new Node3D { Name = "PlayerHumanModel" };
		AddChild(modelRoot);

		AddMeshPart(modelRoot, "CoatBody", new CapsuleMesh { Radius = 0.28f, Height = 1.12f }, new Vector3(0, 0.02f, 0), new Color(0.055f, 0.12f, 0.19f));
		AddMeshPart(modelRoot, "Head", new SphereMesh { Radius = 0.23f, Height = 0.46f, RadialSegments = 16, Rings = 8 }, new Vector3(0, 0.82f, -0.02f), new Color(0.58f, 0.48f, 0.39f));
		AddMeshPart(modelRoot, "HairCap", new SphereMesh { Radius = 0.235f, Height = 0.24f, RadialSegments = 16, Rings = 4 }, new Vector3(0, 0.94f, -0.03f), new Color(0.08f, 0.075f, 0.07f));
		AddMeshPart(modelRoot, "Backpack", new BoxMesh { Size = new Vector3(0.42f, 0.68f, 0.16f) }, new Vector3(0, 0.08f, 0.31f), new Color(0.04f, 0.06f, 0.085f));
		AddMeshPart(modelRoot, "LeftArm", new CapsuleMesh { Radius = 0.075f, Height = 0.82f }, new Vector3(-0.36f, 0.02f, 0.02f), new Color(0.05f, 0.11f, 0.17f), new Vector3(0, 0, -7));
		AddMeshPart(modelRoot, "RightArm", new CapsuleMesh { Radius = 0.075f, Height = 0.82f }, new Vector3(0.36f, 0.02f, 0.02f), new Color(0.05f, 0.11f, 0.17f), new Vector3(0, 0, 7));
		AddMeshPart(modelRoot, "LeftFoot", new BoxMesh { Size = new Vector3(0.18f, 0.11f, 0.36f) }, new Vector3(-0.14f, -0.78f, -0.08f), new Color(0.12f, 0.1f, 0.075f));
		AddMeshPart(modelRoot, "RightFoot", new BoxMesh { Size = new Vector3(0.18f, 0.11f, 0.36f) }, new Vector3(0.14f, -0.78f, -0.08f), new Color(0.12f, 0.1f, 0.075f));
	}

	private static MeshInstance3D AddMeshPart(Node3D parent, string name, Mesh mesh, Vector3 position, Color color, Vector3? rotationDegrees = null)
	{
		var material = new StandardMaterial3D
		{
			AlbedoColor = color,
			Roughness = 0.86f
		};

		var instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			MaterialOverride = material,
			Position = position,
			RotationDegrees = rotationDegrees ?? Vector3.Zero
		};
		parent.AddChild(instance);
		return instance;
	}

	public override void _ExitTree()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			switch (keyEvent.Keycode)
			{
				case Key.F11:
					ToggleFullscreen();
					break;
				case Key.F:
					_flyMode = !_flyMode;
					Velocity = Vector3.Zero;
					UpdateModeLabel();
					break;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 input2D = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		input2D.X += (Input.IsPhysicalKeyPressed(Key.D) ? 1.0f : 0.0f) - (Input.IsPhysicalKeyPressed(Key.A) ? 1.0f : 0.0f);
		input2D.Y += (Input.IsPhysicalKeyPressed(Key.S) ? 1.0f : 0.0f) - (Input.IsPhysicalKeyPressed(Key.W) ? 1.0f : 0.0f);
		input2D = input2D.LimitLength(1.0f);

		Vector3 direction = GetCameraRelativeDirection(input2D);

		if (_flyMode)
		{
			ProcessFlying(direction);
		}
		else
		{
			ProcessWalking(direction, (float)delta);
		}

		if (UseSceneCameraTransform)
		{
			_camera.GlobalTransform = _sceneCameraTransform;
		}
		else
		{
			UpdateCamera((float)delta);
		}
	}

	private Vector3 GetCameraRelativeDirection(Vector2 input2D)
	{
		if (input2D == Vector2.Zero)
		{
			return Vector3.Zero;
		}

		Vector3 forward = -_camera.GlobalTransform.Basis.Z;
		forward.Y = 0.0f;
		forward = forward.Normalized();

		Vector3 right = _camera.GlobalTransform.Basis.X;
		right.Y = 0.0f;
		right = right.Normalized();

		return (right * input2D.X + forward * -input2D.Y).Normalized();
	}

	private void ProcessWalking(Vector3 direction, float delta)
	{
		float speed = Input.IsKeyPressed(Key.Shift) ? RunSpeed : WalkSpeed;
		Vector3 velocity = Velocity;
		velocity.X = direction.X * speed;
		velocity.Z = direction.Z * speed;

		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * delta;
		}
		else if (Input.IsKeyPressed(Key.Space))
		{
			velocity.Y = JumpVelocity;
		}

		Velocity = velocity;
		MoveAndSlide();
		FaceMovementDirection(direction);
	}

	private void ProcessFlying(Vector3 direction)
	{
		float vertical = 0.0f;
		if (Input.IsKeyPressed(Key.Space) || Input.IsKeyPressed(Key.E))
		{
			vertical += 1.0f;
		}
		if (Input.IsKeyPressed(Key.Q) || Input.IsKeyPressed(Key.Ctrl))
		{
			vertical -= 1.0f;
		}

		float speed = Input.IsKeyPressed(Key.Shift) ? FlyFastSpeed : FlySpeed;
		Vector3 motion = direction + Vector3.Up * vertical;
		Velocity = motion.LengthSquared() > 0.0f ? motion.Normalized() * speed : Vector3.Zero;
		MoveAndSlide();
		FaceMovementDirection(direction);
	}

	private void FaceMovementDirection(Vector3 direction)
	{
		if (direction.LengthSquared() <= 0.001f)
		{
			return;
		}

		LookAt(GlobalPosition + direction, Vector3.Up);
	}

	private void UpdateModeLabel()
	{
		if (GetNodeOrNull<Label>("../Interface/ModeLabel") is Label label)
		{
			label.Text = _flyMode ? "MODE: ISO FLY" : "MODE: ISO WALK";
		}
	}

	private void UpdateCameraImmediate()
	{
		Vector3 target = GetCameraTarget();
		_camera.GlobalPosition = target + CameraOffset;
		_camera.LookAt(target, Vector3.Up);
	}

	private void UpdateCamera(float delta)
	{
		Vector3 target = GetCameraTarget();
		Vector3 desiredPosition = target + CameraOffset;
		float weight = 1.0f - Mathf.Exp(-CameraFollowSpeed * delta);
		_camera.GlobalPosition = _camera.GlobalPosition.Lerp(desiredPosition, weight);
		_camera.LookAt(target, Vector3.Up);
	}

	private Vector3 GetCameraTarget()
	{
		return FixedRoomCamera ? RoomViewCenter : GlobalPosition + CameraLookAtOffset;
	}

	private static void ToggleFullscreen()
	{
		if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
	}
}
