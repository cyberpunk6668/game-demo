using Godot;

public partial class TemporalProp : Area2D
{
	[Export] public string PropName = "UniquePropName";
	[Export] public string DisplayName = "Inspectable Prop";
	[Export(PropertyHint.MultilineText)] public string TextDayMinus3 = "";
	[Export(PropertyHint.MultilineText)] public string TextDayZero = "";
	[Export(PropertyHint.MultilineText)] public string TextDayPlus7 = "";
	[Export] public Color ColorDayMinus3 = new(0.8f, 0.7f, 0.45f);
	[Export] public Color ColorDayZero = new(0.55f, 0.65f, 0.8f);
	[Export] public Color ColorDayPlus7 = new(0.55f, 0.4f, 0.75f);

	private Polygon2D? _shape;
	private Label? _label;
	private TimeStateManager? _timeStateManager;

	public override void _Ready()
	{
		_shape = GetNodeOrNull<Polygon2D>("Shape2D");
		_label = GetNodeOrNull<Label>("PropLabel");
		_timeStateManager = GetTree().Root.GetNodeOrNull<TimeStateManager>("Root/TimeStateManager") ?? GetNodeOrNull<TimeStateManager>("/root/Root/TimeStateManager");

		if (_timeStateManager != null)
		{
			_timeStateManager.TimeChanged += OnTimeChanged;
			OnTimeChanged((int)_timeStateManager.CurrentActualDay);
		}
	}

	public override void _ExitTree()
	{
		if (_timeStateManager != null)
		{
			_timeStateManager.TimeChanged -= OnTimeChanged;
		}
	}

	public string Inspect()
	{
		if (_timeStateManager == null)
		{
			return $"{DisplayName}: 时间信号尚未稳定。";
		}

		string body = _timeStateManager.CurrentActualDay switch
		{
			TimeStateManager.TimeDay.DayMinus3 => TextDayMinus3,
			TimeStateManager.TimeDay.DayZero => TextDayZero,
			TimeStateManager.TimeDay.DayPlus7 => TextDayPlus7,
			_ => string.Empty
		};

		return $"{DisplayName}\n{body}";
	}

	private void OnTimeChanged(int hiddenDayIndex)
	{
		var day = (TimeStateManager.TimeDay)hiddenDayIndex;
		if (_shape != null)
		{
			_shape.Color = day switch
			{
				TimeStateManager.TimeDay.DayMinus3 => ColorDayMinus3,
				TimeStateManager.TimeDay.DayZero => ColorDayZero,
				TimeStateManager.TimeDay.DayPlus7 => ColorDayPlus7,
				_ => Colors.White
			};
		}

		if (_label != null)
		{
			_label.Text = DisplayName;
		}
	}
}
