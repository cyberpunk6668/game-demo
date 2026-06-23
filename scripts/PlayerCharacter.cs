using Godot;

public partial class PlayerCharacter : CharacterBody2D
{
	[Export] public float MoveSpeed = 155.0f;

	private AnimationPlayer? _animationPlayer;

	public override void _Ready()
	{
		_animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = direction * MoveSpeed;
		MoveAndSlide();

		if (_animationPlayer != null)
		{
			string animationName = direction == Vector2.Zero ? "Idle" : "Walk";
			if (_animationPlayer.HasAnimation(animationName) && _animationPlayer.CurrentAnimation != animationName)
			{
				_animationPlayer.Play(animationName);
			}
		}
	}

	public void PlayBreakdown()
	{
		if (_animationPlayer != null && _animationPlayer.HasAnimation("Breakdown"))
		{
			_animationPlayer.Play("Breakdown");
		}
	}
}
