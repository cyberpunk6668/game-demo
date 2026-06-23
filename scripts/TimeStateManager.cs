using Godot;
using System;
using System.Collections.Generic;

public partial class TimeStateManager : Node
{
	public enum TimeDay
	{
		DayMinus3 = 0,
		DayZero = 1,
		DayPlus7 = 2
	}

	[Signal]
	public delegate void TimeChangedEventHandler(int hiddenDayIndex);

	[Signal]
	public delegate void SanityChangedEventHandler(float newSanity);

	public TimeDay CurrentActualDay { get; private set; } = TimeDay.DayZero;
	public int CurrentJumpPhase { get; private set; } = 1;
	public float PlayerSanity { get; private set; } = 100.0f;

	private readonly Dictionary<string, Variant> _propStateRegistry = new();
	private readonly Random _random = new();

	public override void _Ready()
	{
		InitializeJumpPhase(CurrentJumpPhase);
	}

	public void InitializeJumpPhase(int phase)
	{
		CurrentJumpPhase = Mathf.Clamp(phase, 1, 4);
		CurrentActualDay = CurrentJumpPhase switch
		{
			1 => TimeDay.DayZero,
			2 => TimeDay.DayMinus3,
			3 => TimeDay.DayPlus7,
			4 => TimeDay.DayZero,
			_ => (TimeDay)_random.Next(0, 3)
		};

		EmitSignal(SignalName.TimeChanged, (int)CurrentActualDay);
	}

	public bool VerifyAnchorDate(int inputDayIndex)
	{
		if (inputDayIndex == (int)CurrentActualDay)
		{
			return true;
		}

		PlayerSanity = Mathf.Max(0.0f, PlayerSanity - 25.0f);
		EmitSignal(SignalName.SanityChanged, PlayerSanity);
		return false;
	}

	public void AdvanceJumpPhase()
	{
		InitializeJumpPhase(CurrentJumpPhase + 1);
	}

	public void RestartLoop()
	{
		PlayerSanity = 100.0f;
		_propStateRegistry.Clear();
		InitializeJumpPhase(1);
	}

	public void SavePropState(string propName, Variant state)
	{
		_propStateRegistry[propName] = state;
	}

	public Variant LoadPropState(string propName)
	{
		return _propStateRegistry.TryGetValue(propName, out Variant state) ? state : default;
	}

	public bool GetOrCreateNoiseFlag(string key, float probabilityTrue)
	{
		if (_propStateRegistry.TryGetValue(key, out Variant stored))
		{
			return stored.AsBool();
		}

		bool value = _random.NextDouble() < probabilityTrue;
		_propStateRegistry[key] = value;
		return value;
	}

	public string GetDayLabel()
	{
		return CurrentActualDay switch
		{
			TimeDay.DayMinus3 => "Day -3 / 事故前三天",
			TimeDay.DayZero => "Day 0 / 事故当天",
			TimeDay.DayPlus7 => "Day +7 / 事故后一周",
			_ => "Unknown"
		};
	}
}
