using Godot;
using System;

public partial class ThirdPersonController : CharacterBody3D
{
	[Export] public float WalkSpeed { get; set; } = 2.6f;
	[Export] public float JogSpeed { get; set; } = 4.8f;
	[Export] public float SprintSpeed { get; set; } = 7.2f;
	[Export] public float Acceleration { get; set; } = 7.0f;
	[Export] public float SprintAcceleration { get; set; } = 4.8f;
	[Export] public float Deceleration { get; set; } = 12.0f;
	[Export] public float RotationSpeed { get; set; } = 12.0f;
	[Export] public float MaxForwardLeanDegrees { get; set; } = 7.0f;
	[Export] public float MaxTurnLeanDegrees { get; set; } = 5.0f;
	[Export] public float JumpVelocity { get; set; } = 4.8f;
	[Export] public float MouseSensitivity { get; set; } = 0.0025f;
	[Export] public float MinCameraPitchDegrees { get; set; } = -35.0f;
	[Export] public float MaxCameraPitchDegrees { get; set; } = 12.0f;
	[Export] public float FallResetHeight { get; set; } = -6.0f;

	private Node3D _visual = null!;
	private Node3D _cameraYaw = null!;
	private Node3D _cameraPitch = null!;
	private AnimationPlayer? _animationPlayer;
	private Vector3 _spawnPosition;
	private float _gravity;
	private float _cameraYawAngle;
	private float _cameraPitchAngle = Mathf.DegToRad(-9.0f);
	private bool _mouseCaptured = true;
	private string _currentAnimation = "";
	private float _visualForwardLean;
	private float _visualTurnLean;
	private Vector3 _previousDirection;

	public override void _Ready()
	{
		_visual = GetNode<Node3D>("Visual");
		_cameraYaw = GetNode<Node3D>("CameraYaw");
		_cameraPitch = GetNode<Node3D>("CameraYaw/CameraPitch");
		_animationPlayer = FindChild("AnimationPlayer", true, false) as AnimationPlayer;
		_spawnPosition = GlobalPosition;
		_gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity").AsDouble();

		SetMouseCapture(true);
		UpdateCameraRotation();
		PlayAnimation("Idle", 0.0);

		if (_animationPlayer == null)
		{
			GD.PushWarning("Third-person character has no AnimationPlayer.");
		}
		else
		{
			GD.Print($"Third-person character loaded {_animationPlayer.GetAnimationList().Length} animations; current animation: {_currentAnimation}.");
		}
	}

	public override void _ExitTree()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion && _mouseCaptured)
		{
			_cameraYawAngle -= mouseMotion.Relative.X * MouseSensitivity;
			_cameraPitchAngle = Mathf.Clamp(
				_cameraPitchAngle - mouseMotion.Relative.Y * MouseSensitivity,
				Mathf.DegToRad(MinCameraPitchDegrees),
				Mathf.DegToRad(MaxCameraPitchDegrees)
			);
			UpdateCameraRotation();
		}

		if (@event is InputEventMouseButton mouseButton
			&& mouseButton.Pressed
			&& mouseButton.ButtonIndex == MouseButton.Left
			&& !_mouseCaptured)
		{
			SetMouseCapture(true);
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Escape)
			{
				SetMouseCapture(!_mouseCaptured);
			}
			else if (keyEvent.Keycode == Key.F11)
			{
				ToggleFullscreen();
			}
		}
	}

	public override void _PhysicsProcess(double deltaValue)
	{
		float delta = (float)deltaValue;
		if (GlobalPosition.Y < FallResetHeight)
		{
			ResetToSpawn();
			return;
		}

		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		input.X += (Input.IsPhysicalKeyPressed(Key.D) ? 1.0f : 0.0f)
			- (Input.IsPhysicalKeyPressed(Key.A) ? 1.0f : 0.0f);
		input.Y += (Input.IsPhysicalKeyPressed(Key.S) ? 1.0f : 0.0f)
			- (Input.IsPhysicalKeyPressed(Key.W) ? 1.0f : 0.0f);
		input = input.LimitLength(1.0f);

		Vector3 cameraForward = -_cameraYaw.GlobalBasis.Z;
		cameraForward.Y = 0.0f;
		cameraForward = cameraForward.Normalized();
		Vector3 cameraRight = _cameraYaw.GlobalBasis.X;
		cameraRight.Y = 0.0f;
		cameraRight = cameraRight.Normalized();

		Vector3 direction = (cameraRight * input.X + cameraForward * -input.Y);
		if (direction.LengthSquared() > 0.001f)
		{
			direction = direction.Normalized();
		}

		bool sprinting = Input.IsKeyPressed(Key.Shift);
		bool walking = Input.IsKeyPressed(Key.Alt);
		float targetSpeed = walking ? WalkSpeed : sprinting ? SprintSpeed : JogSpeed;
		Vector3 targetVelocity = direction * targetSpeed;
		Vector3 velocity = Velocity;
		float moveAcceleration = sprinting ? SprintAcceleration : Acceleration;
		float horizontalChange = (direction.LengthSquared() > 0.001f ? moveAcceleration : Deceleration) * delta;
		velocity.X = Mathf.MoveToward(velocity.X, targetVelocity.X, horizontalChange);
		velocity.Z = Mathf.MoveToward(velocity.Z, targetVelocity.Z, horizontalChange);

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

		if (direction.LengthSquared() > 0.001f)
		{
			float targetAngle = Mathf.Atan2(direction.X, direction.Z);
			float currentAngle = _visual.Rotation.Y;
			float signedTurn = _previousDirection.LengthSquared() > 0.001f
				? Mathf.Atan2(_previousDirection.Cross(direction).Y, _previousDirection.Dot(direction))
				: 0.0f;
			float targetForwardLean = -Mathf.DegToRad(MaxForwardLeanDegrees)
				* Mathf.Clamp(new Vector2(velocity.X, velocity.Z).Length() / SprintSpeed, 0.0f, 1.0f);
			float targetTurnLean = -Mathf.DegToRad(MaxTurnLeanDegrees)
				* Mathf.Clamp(signedTurn / Mathf.DegToRad(45.0f), -1.0f, 1.0f);
			_visualForwardLean = Mathf.Lerp(_visualForwardLean, targetForwardLean, 1.0f - Mathf.Exp(-7.0f * delta));
			_visualTurnLean = Mathf.Lerp(_visualTurnLean, targetTurnLean, 1.0f - Mathf.Exp(-9.0f * delta));
			_visual.Rotation = new Vector3(
				_visualForwardLean,
				Mathf.LerpAngle(currentAngle, targetAngle, 1.0f - Mathf.Exp(-RotationSpeed * delta)),
				_visualTurnLean
			);
			_previousDirection = direction;
		}
		else
		{
			_visualForwardLean = Mathf.Lerp(_visualForwardLean, 0.0f, 1.0f - Mathf.Exp(-9.0f * delta));
			_visualTurnLean = Mathf.Lerp(_visualTurnLean, 0.0f, 1.0f - Mathf.Exp(-9.0f * delta));
			_visual.Rotation = new Vector3(_visualForwardLean, _visual.Rotation.Y, _visualTurnLean);
		}

		UpdateAnimation(direction, walking, sprinting, new Vector2(velocity.X, velocity.Z).Length());
	}

	private void UpdateAnimation(Vector3 direction, bool walking, bool sprinting, float horizontalSpeed)
	{
		if (!IsOnFloor())
		{
			PlayAnimation("Jump");
		}
		else if (direction.LengthSquared() <= 0.001f)
		{
			PlayAnimation("Idle");
		}
		else if (walking)
		{
			PlayAnimation("Walk");
		}
		else if (sprinting)
		{
			PlayAnimation("Sprint");
		}
		else
		{
			PlayAnimation("Jog_Fwd");
		}

		if (_animationPlayer != null)
		{
			float referenceSpeed = walking ? WalkSpeed : sprinting ? SprintSpeed : JogSpeed;
			float targetAnimationSpeed = direction.LengthSquared() > 0.001f
				? Mathf.Clamp(horizontalSpeed / Mathf.Max(referenceSpeed, 0.1f), 0.55f, 1.18f)
				: 1.0f;
			_animationPlayer.SpeedScale = Mathf.Lerp(
				_animationPlayer.SpeedScale,
				targetAnimationSpeed,
				1.0f - Mathf.Exp(-8.0f * (float)GetPhysicsProcessDeltaTime())
			);
		}
	}

	private void PlayAnimation(string requestedName, double blend = 0.18)
	{
		if (_animationPlayer == null)
		{
			return;
		}

		string? animationName = ResolveAnimationName(requestedName);
		if (animationName == null || animationName == _currentAnimation)
		{
			return;
		}

		_animationPlayer.Play(animationName, blend);
		_currentAnimation = animationName;
	}

	private string? ResolveAnimationName(string requestedName)
	{
		if (_animationPlayer == null)
		{
			return null;
		}

		foreach (StringName animationName in _animationPlayer.GetAnimationList())
		{
			string candidate = animationName.ToString();
			if (candidate == requestedName || candidate.EndsWith("/" + requestedName, StringComparison.Ordinal))
			{
				return candidate;
			}
		}

		return null;
	}

	private void UpdateCameraRotation()
	{
		_cameraYaw.Rotation = new Vector3(0.0f, _cameraYawAngle, 0.0f);
		_cameraPitch.Rotation = new Vector3(_cameraPitchAngle, 0.0f, 0.0f);
	}

	private void ResetToSpawn()
	{
		GlobalPosition = _spawnPosition;
		Velocity = Vector3.Zero;
	}

	private void SetMouseCapture(bool captured)
	{
		_mouseCaptured = captured;
		Input.MouseMode = captured ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
	}

	private static void ToggleFullscreen()
	{
		DisplayServer.WindowSetMode(
			DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen
				? DisplayServer.WindowMode.Windowed
				: DisplayServer.WindowMode.Fullscreen
		);
	}
}
