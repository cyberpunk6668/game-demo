using Godot;
using System;
using System.Collections.Generic;

public partial class GameController : Node
{
	private TimeStateManager _timeStateManager = null!;
	private Node2D _mainStage = null!;
	private CanvasModulate _canvasModulate = null!;
	private Node2D _dynamicProps = null!;
	private Node2D _lightSystem = null!;
	private CharacterBody2D _player = null!;
	private CanvasLayer _uiLayer = null!;
	private Label _interactionLabel = null!;
	private ColorRect _inspectionOverlay = null!;
	private Control _calendarMatrix = null!;
	private ColorRect _glitchShaderRect = null!;
	private Label _inspectionText = null!;
	private Label _sanityLabel = null!;
	private Button _submitAnchorButton = null!;
	private TextureProgressBar _countdownBar = null!;

	private readonly List<TemporalProp> _props = new();
	private TemporalProp? _focusedProp;
	private int _selectedAnchorDay = -1;
	private double _calendarTimeLeft = 45.0;
	private bool _calendarActive;
	private float _timePassed;
	private FastNoiseLite _noise = new();

	public override void _Ready()
	{
		_timeStateManager = GetNode<TimeStateManager>("TimeStateManager");
		_mainStage = GetNode<Node2D>("MainStage");
		_canvasModulate = GetNode<CanvasModulate>("MainStage/CanvasModulate");
		_dynamicProps = GetNode<Node2D>("MainStage/Dynamic_Props");
		_lightSystem = GetNode<Node2D>("MainStage/Light_System");
		_player = GetNode<CharacterBody2D>("MainStage/Player_Character");
		_uiLayer = GetNode<CanvasLayer>("UILayer");
		_interactionLabel = GetNode<Label>("UILayer/InteractionLabel");
		_inspectionOverlay = GetNode<ColorRect>("UILayer/InspectionOverlay");
		_calendarMatrix = GetNode<Control>("UILayer/CalendarMatrix");
		_glitchShaderRect = GetNode<ColorRect>("UILayer/GlitchShaderRect");
		_inspectionText = GetNode<Label>("UILayer/InspectionOverlay/InspectionText");
		_sanityLabel = GetNode<Label>("UILayer/SanityLabel");
		_submitAnchorButton = GetNode<Button>("UILayer/CalendarMatrix/SubmitAnchorButton");
		_countdownBar = GetNode<TextureProgressBar>("UILayer/CalendarMatrix/CountdownBar");

		_noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		BuildBlockoutVisuals();
		BindProps();
		BindCalendarButtons();

		_timeStateManager.TimeChanged += OnTimeChanged;
		_timeStateManager.SanityChanged += OnSanityChanged;
		_submitAnchorButton.Pressed += SubmitAnchorDate;

		_calendarMatrix.Visible = false;
		_inspectionOverlay.Visible = false;
		_glitchShaderRect.Color = new Color(1, 1, 1, 0);
		OnTimeChanged((int)_timeStateManager.CurrentActualDay);
		OnSanityChanged(_timeStateManager.PlayerSanity);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.E)
			{
				if (_calendarActive)
				{
					SubmitAnchorDate();
					return;
				}

				if (_focusedProp != null)
				{
					InspectFocusedProp();
				}
				return;
			}

			if (key.Keycode == Key.C)
			{
				OpenCalendarMatrix();
				return;
			}

			if (key.Keycode == Key.Escape)
			{
				_calendarActive = false;
				_calendarMatrix.Visible = false;
				_inspectionOverlay.Visible = false;
				Engine.TimeScale = 1.0;
			}
		}
	}

	public override void _Process(double delta)
	{
		AnimateVoltageLight(delta);
		AnimateInteractionLabel(delta);
		AnimateCalendarTimer(delta);
	}

	private void BuildBlockoutVisuals()
	{
		AddRect(_mainStage, "Floor_Base_Blockout", new Rect2(120, 80, 1040, 560), new Color(0.08f, 0.08f, 0.095f));
		AddRect(_mainStage, "Wall_Back_Blockout", new Rect2(120, 80, 1040, 76), new Color(0.045f, 0.055f, 0.07f));
		AddRect(_mainStage, "Window_Blinds_Blockout", new Rect2(145, 185, 115, 220), new Color(0.04f, 0.075f, 0.1f));
		for (int i = 0; i < 8; i++)
		{
			AddLine(_mainStage, "BlindSlat", new Vector2(150, 205 + i * 22), new Vector2(255, 195 + i * 22), new Color(0.28f, 0.36f, 0.43f, 0.55f), 3);
		}

		AddRect(_dynamicProps, "Bed_Prop_Visual", new Rect2(260, 455, 210, 120), new Color(0.14f, 0.16f, 0.22f));
		AddRect(_dynamicProps, "Desk_Visual", new Rect2(570, 345, 250, 130), new Color(0.19f, 0.11f, 0.055f));
		AddRect(_dynamicProps, "Pill_Table_Visual", new Rect2(245, 575, 150, 56), new Color(0.12f, 0.08f, 0.05f));
		AddRect(_dynamicProps, "Calendar_Wall_Visual", new Rect2(880, 155, 145, 95), new Color(0.13f, 0.12f, 0.1f));
	}

	private void BindProps()
	{
		_props.Clear();
		foreach (Node node in _dynamicProps.GetChildren())
		{
			if (node is TemporalProp prop)
			{
				_props.Add(prop);
				prop.BodyEntered += body => OnPropBodyEntered(prop, body);
				prop.BodyExited += body => OnPropBodyExited(prop, body);
			}
		}
	}

	private void BindCalendarButtons()
	{
		GetNode<Button>("UILayer/CalendarMatrix/DayMinus3Button").Pressed += () => SelectAnchorDate(0);
		GetNode<Button>("UILayer/CalendarMatrix/DayZeroButton").Pressed += () => SelectAnchorDate(1);
		GetNode<Button>("UILayer/CalendarMatrix/DayPlus7Button").Pressed += () => SelectAnchorDate(2);
	}

	private void OnPropBodyEntered(TemporalProp prop, Node2D body)
	{
		if (body == _player)
		{
			_focusedProp = prop;
			_interactionLabel.Text = $"[E] {prop.DisplayName}";
			_interactionLabel.Visible = true;
		}
	}

	private void OnPropBodyExited(TemporalProp prop, Node2D body)
	{
		if (body == _player && _focusedProp == prop)
		{
			_focusedProp = null;
			_interactionLabel.Visible = false;
		}
	}

	private async void InspectFocusedProp()
	{
		if (_focusedProp == null)
		{
			return;
		}

		if (_focusedProp.PropName == "Calendar")
		{
			OpenCalendarMatrix();
			return;
		}

		_inspectionOverlay.Visible = true;
		Engine.TimeScale = 0.5;
		await FadeInspectionOverlay(0.78f);
		await PlayTypewriterText(_inspectionText, _focusedProp.Inspect());
		Engine.TimeScale = 1.0;
	}

	private async System.Threading.Tasks.Task FadeInspectionOverlay(float targetAlpha)
	{
		Color color = _inspectionOverlay.Color;
		for (int i = 0; i <= 10; i++)
		{
			color.A = Mathf.Lerp(0.0f, targetAlpha, i / 10.0f);
			_inspectionOverlay.Color = color;
			await ToSignal(GetTree().CreateTimer(0.025), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private async System.Threading.Tasks.Task PlayTypewriterText(Label label, string fullText)
	{
		label.Text = string.Empty;
		foreach (char c in fullText)
		{
			label.Text += c;
			double delay = c is '。' or '？' or '！' or '，' ? 0.16 : 0.035;
			await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void OpenCalendarMatrix()
	{
		_selectedAnchorDay = -1;
		_calendarTimeLeft = 45.0;
		_calendarActive = true;
		_calendarMatrix.Visible = true;
		_countdownBar.Value = 100;
		UpdateCalendarSelectionText();
	}

	private void SelectAnchorDate(int dayIndex)
	{
		_selectedAnchorDay = dayIndex;
		UpdateCalendarSelectionText();
	}

	private void UpdateCalendarSelectionText()
	{
		string label = _selectedAnchorDay switch
		{
			0 => "已选择：Day -3 / 电影约定 + 欢快留声",
			1 => "已选择：Day 0 / 绝笔道别 + 信号盲音",
			2 => "已选择：Day +7 / 混乱公式 + 警方通告",
			_ => "请选择一个时间锚点"
		};
		GetNode<Label>("UILayer/CalendarMatrix/SelectionLabel").Text = label;
	}

	private void SubmitAnchorDate()
	{
		if (!_calendarActive)
		{
			return;
		}

		_calendarActive = false;
		_calendarMatrix.Visible = false;
		bool correct = _timeStateManager.VerifyAnchorDate(_selectedAnchorDay);
		if (correct)
		{
			TriggerWhiteoutCollapse();
			_timeStateManager.AdvanceJumpPhase();
		}
		else
		{
			TriggerSustainedJumpGlitch();
			if (_player is PlayerCharacter playerCharacter)
			{
				playerCharacter.PlayBreakdown();
			}
		}
	}

	private void AnimateCalendarTimer(double delta)
	{
		if (!_calendarActive)
		{
			return;
		}

		_calendarTimeLeft -= delta;
		_countdownBar.Value = Mathf.Clamp((float)(_calendarTimeLeft / 45.0 * 100.0), 0.0f, 100.0f);
		if (_calendarTimeLeft <= 0)
		{
			_selectedAnchorDay = -1;
			SubmitAnchorDate();
		}
	}

	private void TriggerSustainedJumpGlitch()
	{
		_glitchShaderRect.Color = new Color(0.9f, 0.05f, 0.05f, 0.72f);
		_canvasModulate.Color = new Color(0.35f, 0.04f, 0.04f);
		GetTree().CreateTimer(0.7).Timeout += () =>
		{
			_glitchShaderRect.Color = new Color(0, 0, 0, 0);
			OnTimeChanged((int)_timeStateManager.CurrentActualDay);
		};
	}

	private void TriggerWhiteoutCollapse()
	{
		_glitchShaderRect.Color = new Color(1, 1, 1, 0.95f);
		GetTree().CreateTimer(1.2).Timeout += () =>
		{
			_glitchShaderRect.Color = new Color(1, 1, 1, 0);
			OnTimeChanged((int)_timeStateManager.CurrentActualDay);
		};
	}

	private void OnTimeChanged(int hiddenDayIndex)
	{
		var day = (TimeStateManager.TimeDay)hiddenDayIndex;
		_canvasModulate.Color = day switch
		{
			TimeStateManager.TimeDay.DayMinus3 => new Color(0.92f, 0.86f, 0.72f),
			TimeStateManager.TimeDay.DayZero => new Color(0.56f, 0.68f, 0.85f),
			TimeStateManager.TimeDay.DayPlus7 => new Color(0.42f, 0.32f, 0.52f),
			_ => Colors.White
		};
	}

	private void OnSanityChanged(float newSanity)
	{
		_sanityLabel.Text = $"Sanity: {newSanity:0}";
	}

	private void AnimateVoltageLight(double delta)
	{
		_timePassed += (float)delta * 50.0f;
		var lamp = GetNodeOrNull<PointLight2D>("MainStage/Light_System/DeskLamp");
		if (lamp != null)
		{
			float noiseVal = _noise.GetNoise1D(_timePassed);
			lamp.Energy = Mathf.Lerp(0.2f, 1.5f, (noiseVal + 1.0f) / 2.0f);
		}
	}

	private void AnimateInteractionLabel(double delta)
	{
		if (!_interactionLabel.Visible)
		{
			return;
		}

		float y = 610.0f + Mathf.Sin(Time.GetTicksMsec() / 220.0f) * 4.0f;
		_interactionLabel.Position = new Vector2(560, y);
	}

	private Polygon2D AddRect(Node parent, string name, Rect2 rect, Color color)
	{
		var polygon = new Polygon2D
		{
			Name = name,
			Position = rect.Position,
			Polygon = new[] { Vector2.Zero, new Vector2(rect.Size.X, 0), rect.Size, new Vector2(0, rect.Size.Y) },
			Color = color
		};
		parent.AddChild(polygon);
		return polygon;
	}

	private Line2D AddLine(Node parent, string name, Vector2 from, Vector2 to, Color color, float width)
	{
		var line = new Line2D { Name = name, Points = new[] { from, to }, DefaultColor = color, Width = width, Antialiased = true };
		parent.AddChild(line);
		return line;
	}
}
