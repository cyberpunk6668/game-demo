using Godot;
using System.Collections.Generic;

public enum TimePhase
{
	OpeningApartment = 0,
	AccidentNightFirst = 1,
	ApartmentBefore = 2,
	ApartmentAfter = 3,
	FinalPlatform = 4,
	AcceptedEnding = 5
}

public partial class TimeManager : Node
{
	public static TimeManager? Instance { get; private set; }

	public TimePhase CurrentPhase { get; private set; } = TimePhase.OpeningApartment;
	public int LoopCount { get; private set; } = 1;
	public Dictionary<string, bool> ObjectStates { get; } = new();
	public List<string> MemoryLog { get; } = new();

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		EnsureDefaultStates();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void ResetTimeline()
	{
		CurrentPhase = TimePhase.OpeningApartment;
		LoopCount = 1;
		ObjectStates.Clear();
		MemoryLog.Clear();
		EnsureDefaultStates();
	}

	public void MarkState(string key, bool value)
	{
		ObjectStates[key] = value;
	}

	public bool GetState(string key)
	{
		return ObjectStates.TryGetValue(key, out bool value) && value;
	}

	public void JumpTo(TimePhase phase, string reason)
	{
		CurrentPhase = phase;
		Remember(reason);
		EnsureDefaultStates();
	}

	public void CollapseTo(TimePhase phase, string reason)
	{
		Remember("时间线排异：" + reason);
		JumpTo(phase, reason);
	}

	public void RestartLoop(string reason)
	{
		LoopCount++;
		CurrentPhase = TimePhase.AccidentNightFirst;
		Remember("死循环重启：" + reason);
		EnsureDefaultStates();
	}

	public void AcceptEnding(string reason)
	{
		CurrentPhase = TimePhase.AcceptedEnding;
		Remember("放下：" + reason);
	}

	private void Remember(string entry)
	{
		MemoryLog.Add($"[{LoopCount}:{(int)CurrentPhase}] {entry}");
		if (MemoryLog.Count > 8)
		{
			MemoryLog.RemoveAt(0);
		}
	}

	private void EnsureDefaultStates()
	{
		ObjectStates.TryAdd("DiaryRead", false);
		ObjectStates.TryAdd("KeyHidden", false);
		ObjectStates.TryAdd("CupKnocked", false);
		ObjectStates.TryAdd("MirrorChecked", false);
		ObjectStates.TryAdd("MedicineTaken", false);
		ObjectStates.TryAdd("DoorOpened", false);
	}
}
