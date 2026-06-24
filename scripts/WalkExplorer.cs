using Godot;

public partial class WalkExplorer : CharacterBody3D
{
	[Export] public float WalkSpeed { get; set; } = 3.5f;
	[Export] public float RunSpeed { get; set; } = 6.5f;
	[Export] public float JumpVelocity { get; set; } = 4.3f;
	[Export] public float MouseSensitivity { get; set; } = 0.0022f;
	[Export] public float FlySpeed { get; set; } = 7.0f;
	[Export] public float FlyFastSpeed { get; set; } = 16.0f;

	private Node3D _head = null!;
	private Camera3D _camera = null!;
	private float _pitch;
	private bool _mouseCaptured = true;
	private bool _flyMode;
	private float _gravity;

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity").AsDouble();

		_camera.Current = true;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		UpdateModeLabel();
	}

	public override void _ExitTree()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion && _mouseCaptured)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			_pitch = Mathf.Clamp(_pitch - mouseMotion.Relative.Y * MouseSensitivity, Mathf.DegToRad(-88.0f), Mathf.DegToRad(88.0f));

			Vector3 headRotation = _head.Rotation;
			headRotation.X = _pitch;
			_head.Rotation = headRotation;
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left && !_mouseCaptured)
			{
				SetMouseCapture(true);
			}
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			switch (keyEvent.Keycode)
			{
				case Key.Escape:
					SetMouseCapture(!_mouseCaptured);
					break;
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

		Vector3 direction = Transform.Basis * new Vector3(input2D.X, 0.0f, input2D.Y);
		direction = direction.Normalized();

		if (_flyMode)
		{
			ProcessFlying(direction);
		}
		else
		{
			ProcessWalking(direction, (float)delta);
		}
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
	}

	private void SetMouseCapture(bool captured)
	{
		_mouseCaptured = captured;
		Input.MouseMode = captured ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
	}

	private void UpdateModeLabel()
	{
		if (GetNodeOrNull<Label>("../Interface/ModeLabel") is Label label)
		{
			label.Text = _flyMode ? "MODE: FLY" : "MODE: WALK";
		}
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
