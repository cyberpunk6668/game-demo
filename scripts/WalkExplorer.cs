using Godot;
using System.Collections.Generic;

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
	[Export] public bool BindOriginalHumanModel { get; set; } = true;
	[Export] public string OriginalHumanNodePrefix { get; set; } = "Character_";
	[Export] public float TurnSpeedDegrees { get; set; } = 420.0f;
	[Export] public float TurnLeanDegrees { get; set; } = 6.0f;
	[Export] public float WalkBobAmplitude { get; set; } = 0.035f;
	[Export] public float WalkBobSpeed { get; set; } = 9.0f;

	private Node3D _head = null!;
	private Camera3D _camera = null!;
	private Transform3D _sceneCameraTransform;
	private Node3D? _originalHumanModel;
	private bool _flyMode;
	private float _gravity;
	private float _turnLean;
	private float _walkCycle;
	private bool _isMoving;

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity").AsDouble();
		BindExistingHumanModelToPlayer();

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

	private void BindExistingHumanModelToPlayer()
	{
		if (!BindOriginalHumanModel || _originalHumanModel != null || GetNodeOrNull<Node3D>("OriginalHumanModel") != null)
		{
			return;
		}

		var modelParts = new List<Node3D>();
		CollectOriginalHumanParts(GetParent(), modelParts);
		if (modelParts.Count == 0)
		{
			GD.PushWarning($"No original human model parts found with prefix '{OriginalHumanNodePrefix}'.");
			return;
		}

		Vector3 averagePosition = Vector3.Zero;
		foreach (Node3D part in modelParts)
		{
			averagePosition += part.GlobalPosition;
		}
		averagePosition /= modelParts.Count;
		GlobalPosition = new Vector3(averagePosition.X, GlobalPosition.Y, averagePosition.Z);

		var modelRoot = new Node3D { Name = "OriginalHumanModel" };
		AddChild(modelRoot);

		foreach (Node3D part in modelParts)
		{
			Transform3D originalGlobalTransform = part.GlobalTransform;
			part.GetParent()?.RemoveChild(part);
			modelRoot.AddChild(part);
			part.GlobalTransform = originalGlobalTransform;
		}

		_originalHumanModel = modelRoot;
		GD.Print($"Combined {modelParts.Count} original human model parts under Explorer/OriginalHumanModel. WASD now moves the original model with the player body.");
	}

	private void CollectOriginalHumanParts(Node? node, List<Node3D> output)
	{
		if (node == null)
		{
			return;
		}

		foreach (Node child in node.GetChildren())
		{
			if (child is Node3D node3D && child.Name.ToString().StartsWith(OriginalHumanNodePrefix))
			{
				output.Add(node3D);
			}
			CollectOriginalHumanParts(child, output);
		}
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

		AnimateOriginalHumanModel((float)delta);
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
		//real walk and the and the walk 
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
		SmoothFaceMovementDirection(direction, delta);
		_isMoving = direction.LengthSquared() > 0.001f;
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
		SmoothFaceMovementDirection(direction, (float)GetPhysicsProcessDeltaTime());
		_isMoving = motion.LengthSquared() > 0.001f;
	}

	private void SmoothFaceMovementDirection(Vector3 direction, float delta)
	{
		if (direction.LengthSquared() <= 0.001f)
		{
			_turnLean = Mathf.MoveToward(_turnLean, 0.0f, delta * 8.0f);
			return;
		}

		Vector3 currentForward = -GlobalTransform.Basis.Z;
		currentForward.Y = 0.0f;
		currentForward = currentForward.Normalized();

		Vector3 targetForward = direction;
		targetForward.Y = 0.0f;
		targetForward = targetForward.Normalized();

		float signedAngle = Mathf.Atan2(currentForward.Cross(targetForward).Y, currentForward.Dot(targetForward));
		float maxStep = Mathf.DegToRad(TurnSpeedDegrees) * delta;
		float step = Mathf.Clamp(signedAngle, -maxStep, maxStep);
		RotateY(step);

		float targetLean = Mathf.Clamp(signedAngle / Mathf.DegToRad(90.0f), -1.0f, 1.0f);
		_turnLean = Mathf.Lerp(_turnLean, targetLean, 1.0f - Mathf.Exp(-10.0f * delta));
	}

	private void AnimateOriginalHumanModel(float delta)
	{
		if (_originalHumanModel == null)
		{
			return;
		}

		if (_isMoving)
		{
			_walkCycle += delta * WalkBobSpeed;
		}
		else
		{
			_walkCycle = Mathf.Lerp(_walkCycle, 0.0f, 1.0f - Mathf.Exp(-6.0f * delta));
		}

		float bob = _isMoving ? Mathf.Sin(_walkCycle) * WalkBobAmplitude : 0.0f;
		float leanRadians = Mathf.DegToRad(TurnLeanDegrees) * _turnLean;

		_originalHumanModel.Position = new Vector3(0.0f, bob, 0.0f);
		_originalHumanModel.Rotation = new Vector3(0.0f, 0.0f, -leanRadians);
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
