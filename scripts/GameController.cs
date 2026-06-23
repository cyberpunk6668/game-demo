using Godot;
using System;
using System.Collections.Generic;

public partial class GameController : Node2D
{
	private Node2D _worldRoot = null!;
	private Control _hudRoot = null!;
	private VBoxContainer _actions = null!;
	private Label _titleLabel = null!;
	private Label _phaseLabel = null!;
	private Label _bodyLabel = null!;
	private Label _memoryLabel = null!;
	private ColorRect _glitchOverlay = null!;
	private AudioStreamPlayer _hum = null!;
	private readonly List<Node> _phaseNodes = new();
	private readonly List<Node> _hudEffectNodes = new();
	private double _glitchTime;

	public override void _Ready()
	{
		EnsureTimeManager();
		_worldRoot = new Node2D { Name = "WorldRoot2D" };
		AddChild(_worldRoot);
		CreateHud();
		_hum = new AudioStreamPlayer { Name = "AmbientLoopAnchor2D", VolumeDb = -80 };
		AddChild(_hum);
		RenderPhase();
	}

	public override void _Process(double delta)
	{
		AnimateGlitch(delta);
		AnimateWorld();
		AnimateHudEffects();
	}

	public override void _ExitTree()
	{
		if (_hum != null)
		{
			_hum.Stop();
			_hum.Stream = null;
		}
	}

	private TimeManager Timeline => TimeManager.Instance!;

	private void EnsureTimeManager()
	{
		if (TimeManager.Instance == null)
		{
			AddChild(new TimeManager { Name = "TimeManager" });
		}
	}

	private void CreateHud()
	{
		var canvas = new CanvasLayer { Name = "HUD" };
		AddChild(canvas);

		_hudRoot = new Control { Name = "HudRoot", AnchorRight = 1, AnchorBottom = 1 };
		canvas.AddChild(_hudRoot);

		var panel = new PanelContainer
		{
			Name = "StoryPanel",
			AnchorLeft = 0.03f,
			AnchorTop = 0.04f,
			AnchorRight = 0.43f,
			AnchorBottom = 0.96f
		};
		_hudRoot.AddChild(panel);

		var margin = new MarginContainer { Name = "Margin" };
		margin.AddThemeConstantOverride("margin_left", 18);
		margin.AddThemeConstantOverride("margin_top", 18);
		margin.AddThemeConstantOverride("margin_right", 18);
		margin.AddThemeConstantOverride("margin_bottom", 18);
		panel.AddChild(margin);

		var stack = new VBoxContainer { Name = "StoryStack" };
		stack.AddThemeConstantOverride("separation", 12);
		margin.AddChild(stack);

		_titleLabel = CreateLabel("Title", 26, Colors.White, true);
		_phaseLabel = CreateLabel("Phase", 15, new Color(0.75f, 0.85f, 1.0f), false);
		_bodyLabel = CreateLabel("Body", 17, new Color(0.92f, 0.93f, 0.95f), false);
		_bodyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_memoryLabel = CreateLabel("Memory", 13, new Color(0.72f, 0.76f, 0.82f), false);
		_memoryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_actions = new VBoxContainer { Name = "Actions" };
		_actions.AddThemeConstantOverride("separation", 8);

		stack.AddChild(_titleLabel);
		stack.AddChild(_phaseLabel);
		stack.AddChild(_bodyLabel);
		stack.AddChild(_actions);
		stack.AddChild(_memoryLabel);

		_glitchOverlay = new ColorRect
		{
			Name = "GlitchOverlay2D",
			AnchorRight = 1,
			AnchorBottom = 1,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.3f, 0.85f, 1.0f, 0.0f),
			ZIndex = 20
		};
		_hudRoot.AddChild(_glitchOverlay);
	}

	private Label CreateLabel(string name, int size, Color color, bool bold)
	{
		var label = new Label { Name = name, Text = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		if (bold)
		{
			label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.75f));
			label.AddThemeConstantOverride("shadow_offset_x", 2);
			label.AddThemeConstantOverride("shadow_offset_y", 2);
		}
		return label;
	}

	private void RenderPhase()
	{
		ClearPhase();
		ClearActions();

		switch (Timeline.CurrentPhase)
		{
			case TimePhase.OpeningApartment:
				Timeline.MarkState("CupKnocked", true);
				RenderApartment2D("OpeningApartment_2D", opening: true, beforeAccident: false);
				SetStory("第一场：压抑单间公寓", "2D Opening / 乱码日历与打翻的杯子", "冷青色的室内光压在墙上，唯一的暖色来自忽明忽暗的台灯。木桌上的陶瓷杯已经倒下，干涸水痕像被时间擦除过的证据。墙上的电子钟/日历只剩乱码、撕裂线和无法读取的日期。", ("观察打翻的杯子", () => MarkAndRefresh("CupKnocked", "杯沿是干的，水痕却像刚刚停止流动。")), ("靠近乱码日历", () => MarkAndRefresh("DiaryRead", "数字在 88:?? 与一串错误日期之间撕裂，像有人把时间揉皱后钉在墙上。")), ("进入时间回环", () => ForceJump(TimePhase.AccidentNightFirst, "房间里的地铁低鸣突然贴近耳膜。你被抛向事故当晚的月台。")));
				break;
			case TimePhase.AccidentNightFirst:
				RenderPlatform2D(finalVisit: false, accepted: false);
				SetStory("第一跃：事故当晚", "2D 月台 / 强制干预", "雨声像坏掉的磁带一样循环。她站在黄线外侧，列车灯从雾里刺出来。你以为只要跑得够快、说对一句话，就能把这一晚扳回正轨。", ("冲过去拉住她", () => ForceJump(TimePhase.ApartmentBefore, "你扑向边缘。玻璃一样的裂纹爬满视野，世界把你弹回事故前三天。")), ("大喊她的名字", () => ForceJump(TimePhase.ApartmentBefore, "她回头了，可列车声吞没了回答。时间线崩裂，日期倒卷。")));
				break;
			case TimePhase.ApartmentBefore:
				RenderApartment2D("ApartmentBefore_2D", opening: false, beforeAccident: true);
				SetStory("第二跃：事故前三天", "2D 公寓 / 搜查与藏钥匙", "房间还保留着生活的温度。电子钟正常跳动，杯子里的水没有干，日记摊在桌角。你开始搜寻所谓的“线索”，像是在审问一段本不该被改写的过去。", ("阅读日记碎片", () => MarkAndRefresh("DiaryRead", "日记里没有谜底，只有一句反复涂黑的话：‘别再替我决定。’")), ("藏起门口钥匙", () => MarkAndRefresh("KeyHidden", "钥匙被你塞进沙发缝。下一秒，门锁自己响了一声，像在嘲笑。")), ("打翻水杯", () => MarkAndRefresh("CupKnocked", "水杯倒下，水迹却逆着桌面爬回杯口。")), ("强行改变过去", () => ForceJump(TimePhase.ApartmentAfter, "你越想修正，房间越像旧录像一样卡顿。画面被撕开，事故已经发生后一周。")));
				break;
			case TimePhase.ApartmentAfter:
				RenderApartment2D("ApartmentAfter_2D", opening: false, beforeAccident: false);
				SetStory("第三跃：事故后一周", "2D 公寓 / 空房间与记忆手术", "公寓空得不自然。电子钟显示乱码，镜子里的你额角有一道细小伤疤。药瓶标签写着：‘记忆干预后按需服用。’你终于意识到，被困住的不只是时间。", ("照镜子看伤疤", () => MarkAndRefresh("MirrorChecked", "伤疤发烫。你想起自己主动要求忘掉这一晚，又一次主动把它挖开。")), ("服药维持理智", () => MarkAndRefresh("MedicineTaken", "苦味压下耳鸣，远处地铁的轰鸣却更清楚了。")), ("推开公寓大门", () => ForceJump(TimePhase.FinalPlatform, "门外不是走廊。铁轨、雨、黄线和刺眼车灯一起涌进来。")));
				break;
			case TimePhase.FinalPlatform:
				RenderPlatform2D(finalVisit: true, accepted: false);
				SetStory("第四跃：事故当晚", "2D 月台 / 最后的内心抉择", "一切回到原点，但这一次你知道：循环并不是给你一次完美操作的机会，而是在逼你承认有些遗憾不能被胜利条件覆盖。她仍站在边缘。你的手在发抖。", ("伸手拉她", () => RestartLoop()), ("闭眼转身", () => AcceptEnding()));
				break;
			case TimePhase.AcceptedEnding:
				RenderPlatform2D(finalVisit: false, accepted: true);
				SetStory("通关：时间继续向前", "2D 清空月台 / 接纳遗憾", "列车驶过。没有乱码，没有碎裂，也没有新的跳跃。雨停在半空，又终于落下。月台空了，但时间第一次没有回头。", ("重新体验循环", () => { Timeline.ResetTimeline(); RenderPhase(); }));
				break;
		}

		RenderMemoryLog();
	}

	private void SetStory(string title, string phase, string body, params (string Text, Action Action)[] actions)
	{
		_titleLabel.Text = title;
		_phaseLabel.Text = $"{phase}   ·   第 {Timeline.LoopCount} 次循环";
		_bodyLabel.Text = body;
		foreach (var action in actions)
		{
			var button = new Button { Text = action.Text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.None };
			button.Pressed += action.Action;
			_actions.AddChild(button);
		}
	}

	private void ForceJump(TimePhase nextPhase, string reason)
	{
		_glitchTime = 1.0;
		Timeline.CollapseTo(nextPhase, reason);
		RenderPhase();
	}

	private void RestartLoop()
	{
		_glitchTime = 1.4;
		Timeline.RestartLoop("你再次伸手，世界像程序报错一样强制重启。救赎被误认为胜利，于是循环从头开始。");
		RenderPhase();
	}

	private void AcceptEnding()
	{
		_glitchTime = 0.2;
		Timeline.AcceptEnding("你没有再替她选择。你只是承认了失去，并让时间继续。");
		RenderPhase();
	}

	private void MarkAndRefresh(string key, string memory)
	{
		Timeline.MarkState(key, true);
		Timeline.JumpTo(Timeline.CurrentPhase, memory);
		RenderPhase();
	}

	private void RenderMemoryLog()
	{
		string flags = $"日记:{Flag("DiaryRead")}  钥匙:{Flag("KeyHidden")}  水杯:{Flag("CupKnocked")}\n" + $"镜子:{Flag("MirrorChecked")}  药:{Flag("MedicineTaken")}  门:{Flag("DoorOpened")}";
		_memoryLabel.Text = "跨时间物品状态\n" + flags + "\n\n记忆残响\n" + (Timeline.MemoryLog.Count == 0 ? "还没有可相信的记忆。" : string.Join("\n", Timeline.MemoryLog));
	}

	private string Flag(string key) => Timeline.GetState(key) ? "已改变" : "未改变";

	private void RenderApartment2D(string sceneName, bool opening, bool beforeAccident)
	{
		var room = AddPhaseNode(new Node2D { Name = sceneName });
		Color wallColor = opening ? new Color(0.035f, 0.09f, 0.095f) : beforeAccident ? new Color(0.16f, 0.14f, 0.13f) : new Color(0.05f, 0.075f, 0.11f);
		Color floorColor = opening ? new Color(0.07f, 0.065f, 0.06f) : beforeAccident ? new Color(0.16f, 0.13f, 0.1f) : new Color(0.055f, 0.065f, 0.08f);

		AddRect(room, "Apartment_Backdrop", new Rect2(430, 72, 720, 600), new Color(0.006f, 0.01f, 0.018f));
		AddRect(room, "Apartment_BackWall", new Rect2(430, 72, 720, 392), wallColor);
		AddRect(room, "Apartment_Floor", new Rect2(430, 464, 720, 210), floorColor);
		AddLine(room, "Apartment_Baseboard", new Vector2(430, 464), new Vector2(1150, 464), new Color(0.02f, 0.025f, 0.03f), 5);
		AddRect(room, "Desk", new Rect2(530, 410, 245, 52), new Color(0.2f, 0.12f, 0.06f));
		AddRect(room, "DeskShadow", new Rect2(530, 462, 245, 36), new Color(0.055f, 0.035f, 0.025f, 0.9f));
		AddRect(room, "Sofa", new Rect2(855, 392, 230, 82), beforeAccident ? new Color(0.16f, 0.2f, 0.25f) : new Color(0.07f, 0.09f, 0.12f));
		AddRect(room, "Door", new Rect2(1030, 185, 75, 225), beforeAccident ? new Color(0.22f, 0.14f, 0.08f) : new Color(0.015f, 0.02f, 0.04f));
		AddRect(room, "Mirror", new Rect2(455, 180, 72, 175), Timeline.GetState("MirrorChecked") ? new Color(0.45f, 0.7f, 0.9f, 0.62f) : new Color(0.16f, 0.28f, 0.34f, 0.52f));
		AddCalendar(room, opening || !beforeAccident ? "88:??\n2█/##/??" : "03 DAYS\nBEFORE", opening || !beforeAccident);
		AddCup(room);
		AddDiaryPages(room, beforeAccident);
		AddLamp(room, opening, beforeAccident);
		if (opening || !beforeAccident) AddApartmentGlitches(room);
	}

	private void AddCalendar(Node2D room, string text, bool glitched)
	{
		AddRect(room, "DigitalClockCalendar_Frame", new Rect2(730, 165, 185, 82), new Color(0.012f, 0.014f, 0.018f));
		AddRect(room, "DigitalClockCalendar_Screen", new Rect2(742, 177, 161, 58), new Color(0.005f, 0.008f, 0.012f));
		AddText(room, text, new Vector2(760, 184), glitched ? new Color(0.55f, 1.0f, 0.95f) : new Color(0.95f, 0.78f, 0.42f), 21, "DigitalClockCalendar_Text", glitched ? "glitch_digits" : null);
		if (glitched)
		{
			AddRect(room, "CalendarRGBGhost_Red", new Rect2(724, 174, 184, 4), new Color(1.0f, 0.05f, 0.08f, 0.36f), "world_stutter");
			AddRect(room, "CalendarRGBGhost_Cyan", new Rect2(738, 232, 170, 4), new Color(0.0f, 0.85f, 1.0f, 0.36f), "world_stutter");
		}
	}

	private void AddCup(Node2D room)
	{
		bool knocked = Timeline.GetState("CupKnocked");
		var cup = AddRect(room, "Cup", knocked ? new Rect2(610, 385, 52, 18) : new Rect2(625, 362, 32, 42), knocked ? new Color(0.72f, 0.76f, 0.74f) : Colors.White);
		cup.Rotation = knocked ? -0.18f : 0.0f;
		if (knocked) AddEllipse(room, "DryWaterStain", new Vector2(690, 397), new Vector2(76, 18), new Color(0.28f, 0.34f, 0.32f, 0.58f));
	}

	private void AddDiaryPages(Node2D room, bool beforeAccident)
	{
		for (int i = 0; i < 7; i++)
		{
			var page = AddRect(room, "TornDiaryPage_2D", new Rect2(545 + i * 27, 370 + (i % 2) * 15, 44, 30), beforeAccident ? new Color(0.78f, 0.72f, 0.58f) : new Color(0.45f, 0.43f, 0.38f));
			page.Rotation = -0.28f + i * 0.08f;
			AddLine(room, "DiaryInkLine", page.Position + new Vector2(8, 13), page.Position + new Vector2(34, 13), new Color(0.03f, 0.025f, 0.02f, 0.8f), 2);
		}
	}

	private void AddLamp(Node2D room, bool opening, bool beforeAccident)
	{
		Color lightColor = beforeAccident ? new Color(1.0f, 0.68f, 0.28f, 0.32f) : new Color(0.12f, 0.75f, 0.9f, 0.23f);
		AddEllipse(room, "LampGlow_2D", new Vector2(590, 330), new Vector2(160, 120), opening ? new Color(1.0f, 0.35f, 0.08f, 0.22f) : lightColor, "flicker_lamp");
		AddRect(room, "DeskLampStem_2D", new Rect2(586, 335, 8, 62), new Color(0.03f, 0.025f, 0.018f));
		AddRect(room, "DeskLampShade_2D", new Rect2(555, 315, 68, 32), new Color(0.85f, 0.38f, 0.1f));
	}

	private void AddApartmentGlitches(Node2D room)
	{
		for (int i = 0; i < 12; i++)
		{
			var tear = AddRect(room, "ApartmentTemporalTear_2D", new Rect2(480 + i * 52, 135 + (i % 5) * 62, 45 + (i % 3) * 34, 4), new Color(0.0f, 0.9f, 1.0f, 0.35f), "world_stutter");
			tear.Rotation = -0.12f + i * 0.03f;
		}
	}

	private void RenderPlatform2D(bool finalVisit, bool accepted)
	{
		if (!accepted) RenderTemporalChaosHud(finalVisit);
		var platform = AddPhaseNode(new Node2D { Name = accepted ? "EmptyPlatformAfterAcceptance_2D" : "AccidentNightPlatform_2D" });
		AddPlatformGeometry(platform, accepted);
		if (accepted)
		{
			AddText(platform, "时间恢复向前", new Vector2(760, 250), new Color(0.85f, 0.95f, 1.0f), 34, "AcceptanceText");
			return;
		}
		AddRain(platform);
		AddTrainLight(platform, finalVisit);
		AddWarningTrigger(platform);
		AddUmbrella(platform);
		AddPlatformGlitches(platform);
		AddRect(platform, "HerSilhouette_AtYellowLine_2D", new Rect2(915, 344, 34, 118), finalVisit ? new Color(0.9f, 0.86f, 0.78f, 0.82f) : new Color(0.55f, 0.62f, 0.74f, 0.72f));
	}

	private void AddPlatformGeometry(Node2D platform, bool accepted)
	{
		AddRect(platform, "NightSkyMist_Backdrop_2D", new Rect2(430, 52, 760, 560), accepted ? new Color(0.08f, 0.09f, 0.1f) : new Color(0.01f, 0.015f, 0.025f));
		AddRect(platform, "HeavyFogOccluder_2D", new Rect2(440, 72, 735, 390), accepted ? new Color(0.28f, 0.3f, 0.31f, 0.22f) : new Color(0.08f, 0.13f, 0.2f, 0.48f));
		AddPolygon(platform, "PlatformPerspective_2D", new[] { new Vector2(430, 430), new Vector2(1160, 355), new Vector2(1185, 535), new Vector2(430, 650) }, accepted ? new Color(0.15f, 0.16f, 0.17f) : new Color(0.05f, 0.06f, 0.072f));
		AddPolygon(platform, "TrackBed_2D", new[] { new Vector2(430, 650), new Vector2(1185, 535), new Vector2(1185, 690), new Vector2(430, 690) }, accepted ? new Color(0.08f, 0.09f, 0.1f) : new Color(0.006f, 0.009f, 0.014f));
		AddLine(platform, "RustyRail_Left_2D", new Vector2(520, 676), new Vector2(1090, 520), accepted ? new Color(0.36f, 0.36f, 0.34f) : new Color(0.45f, 0.24f, 0.1f), 7);
		AddLine(platform, "RustyRail_Right_2D", new Vector2(655, 690), new Vector2(1135, 520), accepted ? new Color(0.36f, 0.36f, 0.34f) : new Color(0.45f, 0.24f, 0.1f), 7);
		for (int i = 0; i < 12; i++)
		{
			float t = i / 11.0f;
			AddLine(platform, "WetRailSleeper_2D", new Vector2(500 + t * 570, 662 - t * 142), new Vector2(690 + t * 430, 690 - t * 165), new Color(0.08f, 0.075f, 0.065f), 4);
		}
		for (int i = 0; i < 18; i++) AddRect(platform, "BrightYellowTactileWarningTile_2D", new Rect2(450 + i * 39, 405 - i * 3.3f, 34, 22), accepted ? new Color(0.62f, 0.57f, 0.26f) : new Color(1.0f, 0.85f, 0.05f));
		AddLine(platform, "SharpFocus_YellowPlatformLine_2D", new Vector2(430, 430), new Vector2(1160, 355), accepted ? new Color(0.68f, 0.62f, 0.32f) : new Color(1.0f, 0.9f, 0.05f), 5);
	}

	private void AddRain(Node2D platform)
	{
		for (int i = 0; i < 90; i++) AddLine(platform, "FallingRain_2D", new Vector2(430 + (i * 37) % 760, 60 + (i * 53) % 560), new Vector2(424 + (i * 37) % 760, 96 + (i * 53) % 560), new Color(0.55f, 0.75f, 1.0f, 0.38f), 2, "falling_rain");
		for (int i = 0; i < 24; i++) AddLine(platform, "ReverseGravityRain_2D", new Vector2(455 + (i * 71) % 700, 280 + (i * 41) % 270), new Vector2(459 + (i * 71) % 700, 238 + (i * 41) % 270), new Color(0.75f, 0.95f, 1.0f, 0.62f), 3, "reverse_rain");
	}

	private void AddTrainLight(Node2D platform, bool finalVisit)
	{
		AddEllipse(platform, "BlindingTrainHeadlight_Core_2D", new Vector2(1135, 205), finalVisit ? new Vector2(90, 90) : new Vector2(68, 68), new Color(0.88f, 0.95f, 1.0f, 0.95f), "train_light_core");
		AddPolygon(platform, "TrainHeadlightBeam_2D", new[] { new Vector2(1110, 210), new Vector2(430, 372), new Vector2(430, 470), new Vector2(1110, 245) }, new Color(0.58f, 0.78f, 1.0f, finalVisit ? 0.24f : 0.18f), "train_beam");
		for (int i = 0; i < 6; i++) AddLine(platform, "MotionBlurTrainLight_2D", new Vector2(1010 + i * 20, 198 + i * 5), new Vector2(1118 + i * 8, 198 + i * 5), new Color(0.72f, 0.86f, 1.0f, 0.3f - i * 0.03f), 4, "train_light_streak");
	}

	private void AddWarningTrigger(Node2D platform)
	{
		var trigger = new Area2D { Name = "YellowLineChoiceTrigger_Area2D" };
		trigger.AddChild(new CollisionShape2D { Name = "YellowLineChoiceTriggerShape", Position = new Vector2(800, 410), Shape = new RectangleShape2D { Size = new Vector2(760, 68) } });
		platform.AddChild(trigger);
		AddPolygon(platform, "GlowingArea2DTriggerZone_PixelEdges", new[] { new Vector2(430, 418), new Vector2(1160, 343), new Vector2(1167, 377), new Vector2(430, 454) }, new Color(0.1f, 0.72f, 1.0f, 0.12f), "trigger_glow");
		for (int i = 0; i < 32; i++) AddRect(platform, "PixelatedArea2DEdgeMarker", new Rect2(442 + i * 22, 420 - i * 2.2f, 8, 5), new Color(0.2f, 0.9f, 1.0f, 0.6f), "trigger_pixel_edge");
	}

	private void AddUmbrella(Node2D platform)
	{
		AddLine(platform, "DiscardedUmbrella_Shaft_2D", new Vector2(595, 472), new Vector2(705, 520), new Color(0.08f, 0.08f, 0.09f), 4);
		for (int i = 0; i < 6; i++) AddLine(platform, "CollapsedUmbrella_Rib_2D", new Vector2(620, 480), new Vector2(548 + i * 28, 520 + (i % 2) * 10), new Color(0.01f, 0.02f, 0.035f, 0.85f), 5);
	}

	private void AddPlatformGlitches(Node2D platform)
	{
		for (int i = 0; i < 11; i++)
		{
			var tear = AddRect(platform, "WorldSpaceTemporalStutter_2D", new Rect2(470 + i * 62, 260 + (i % 4) * 52, 42 + (i % 3) * 36, 4), new Color(0.2f, 0.88f, 1.0f, 0.34f), "world_stutter");
			tear.Rotation = -0.08f + i * 0.025f;
		}
		AddLine(platform, "DuplicatedYellowLine_StutterGhost_Red_2D", new Vector2(430, 437), new Vector2(1160, 363), new Color(1.0f, 0.08f, 0.06f, 0.32f), 3);
		AddLine(platform, "DuplicatedYellowLine_StutterGhost_Cyan_2D", new Vector2(430, 424), new Vector2(1160, 348), new Color(0.0f, 0.82f, 1.0f, 0.28f), 3);
	}

	private void RenderTemporalChaosHud(bool finalVisit)
	{
		AddHudEffectNode(new ColorRect { Name = "CinematicLetterboxTop_2D", AnchorRight = 1, OffsetBottom = 54, MouseFilter = Control.MouseFilterEnum.Ignore, Color = new Color(0, 0, 0, 0.7f), ZIndex = 8 });
		AddHudEffectNode(new ColorRect { Name = "CinematicLetterboxBottom_2D", AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetTop = -54, MouseFilter = Control.MouseFilterEnum.Ignore, Color = new Color(0, 0, 0, 0.7f), ZIndex = 8 });
		for (int i = 0; i < 7; i++)
		{
			var bar = AddHudEffectNode(new ColorRect { Name = "VhsTrackingError_ScreenTearBar_2D", AnchorRight = 1, OffsetTop = 70 + i * 58, OffsetBottom = 73 + i * 58, MouseFilter = Control.MouseFilterEnum.Ignore, Color = new Color(0.2f, 0.85f, 1.0f, finalVisit ? 0.18f : 0.11f), ZIndex = 9 });
			bar.SetMeta("vhs_bar", true);
		}
		for (int i = 0; i < 26; i++)
		{
			bool leftCorner = i % 2 == 0;
			var noise = AddHudEffectNode(new ColorRect { Name = "StaticNoise_CornerPixelBlock_2D", AnchorLeft = leftCorner ? 0 : 1, AnchorTop = i % 4 < 2 ? 0 : 1, AnchorRight = leftCorner ? 0 : 1, AnchorBottom = i % 4 < 2 ? 0 : 1, OffsetLeft = leftCorner ? 8 + (i % 7) * 18 : -120 + (i % 7) * 14, OffsetTop = i % 4 < 2 ? 18 + (i % 5) * 14 : -96 + (i % 5) * 14, OffsetRight = leftCorner ? 20 + (i % 7) * 18 : -106 + (i % 7) * 14, OffsetBottom = i % 4 < 2 ? 30 + (i % 5) * 14 : -84 + (i % 5) * 14, MouseFilter = Control.MouseFilterEnum.Ignore, Color = new Color(0.78f, 0.92f, 1.0f, 0.16f), ZIndex = 9 });
			noise.SetMeta("vhs_noise", true);
		}
	}

	private Polygon2D AddRect(Node parent, string name, Rect2 rect, Color color, string? meta = null)
	{
		var polygon = new Polygon2D { Name = name, Position = rect.Position, Polygon = new[] { Vector2.Zero, new Vector2(rect.Size.X, 0), rect.Size, new Vector2(0, rect.Size.Y) }, Color = color };
		if (meta != null) polygon.SetMeta(meta, true);
		parent.AddChild(polygon);
		return polygon;
	}

	private Polygon2D AddPolygon(Node parent, string name, Vector2[] points, Color color, string? meta = null)
	{
		var polygon = new Polygon2D { Name = name, Polygon = points, Color = color };
		if (meta != null) polygon.SetMeta(meta, true);
		parent.AddChild(polygon);
		return polygon;
	}

	private Line2D AddLine(Node parent, string name, Vector2 from, Vector2 to, Color color, float width, string? meta = null)
	{
		var line = new Line2D { Name = name, Points = new[] { from, to }, DefaultColor = color, Width = width, Antialiased = true };
		if (meta != null) line.SetMeta(meta, true);
		parent.AddChild(line);
		return line;
	}

	private Polygon2D AddEllipse(Node parent, string name, Vector2 center, Vector2 radius, Color color, string? meta = null)
	{
		Vector2[] points = new Vector2[40];
		for (int i = 0; i < points.Length; i++)
		{
			float angle = Mathf.Tau * i / points.Length;
			points[i] = center + new Vector2(Mathf.Cos(angle) * radius.X, Mathf.Sin(angle) * radius.Y);
		}
		return AddPolygon(parent, name, points, color, meta);
	}

	private Label AddText(Node parent, string text, Vector2 position, Color color, int size, string name, string? meta = null)
	{
		var label = new Label { Name = name, Text = text, Position = position, Modulate = color };
		label.AddThemeFontSizeOverride("font_size", size);
		if (meta != null) label.SetMeta(meta, true);
		parent.AddChild(label);
		return label;
	}

	private T AddPhaseNode<T>(T node) where T : Node
	{
		_worldRoot.AddChild(node);
		_phaseNodes.Add(node);
		return node;
	}

	private T AddHudEffectNode<T>(T node) where T : Node
	{
		_hudRoot.AddChild(node);
		_hudEffectNodes.Add(node);
		return node;
	}

	private void ClearPhase()
	{
		foreach (var node in _phaseNodes) node.QueueFree();
		_phaseNodes.Clear();
		ClearHudEffects();
	}

	private void ClearHudEffects()
	{
		foreach (var node in _hudEffectNodes) node.QueueFree();
		_hudEffectNodes.Clear();
	}

	private void ClearActions()
	{
		foreach (var child in _actions.GetChildren()) child.QueueFree();
	}

	private void AnimateGlitch(double delta)
	{
		if (_glitchTime <= 0)
		{
			_glitchOverlay.Color = new Color(0.3f, 0.85f, 1.0f, 0.0f);
			return;
		}
		_glitchTime -= delta;
		float alpha = (float)Math.Clamp(_glitchTime, 0.0, 1.0) * 0.38f;
		float hueShift = (float)(Math.Sin(Time.GetTicksMsec() / 45.0) * 0.08 + 0.5);
		_glitchOverlay.Color = new Color(0.2f + hueShift, 0.85f, 1.0f - hueShift * 0.3f, alpha);
	}

	private void AnimateHudEffects()
	{
		double t = Time.GetTicksMsec() / 1000.0;
		for (int i = 0; i < _hudEffectNodes.Count; i++)
		{
			if (_hudEffectNodes[i] is not ColorRect rect) continue;
			if (rect.HasMeta("vhs_bar"))
			{
				Color color = rect.Color;
				color.A = 0.08f + (float)Math.Abs(Math.Sin(t * 8.0 + i)) * 0.16f;
				rect.Color = color;
				rect.Position = new Vector2((float)Math.Sin(t * 17.0 + i * 0.7) * 18.0f, rect.Position.Y);
			}
			if (rect.HasMeta("vhs_noise")) rect.Visible = ((int)(t * 24.0 + i) % 5) != 0;
		}
	}

	private void AnimateWorld()
	{
		foreach (var node in _worldRoot.GetChildren()) AnimateNodeTree(node);
	}

	private void AnimateNodeTree(Node node)
	{
		double t = Time.GetTicksMsec() / 1000.0;
		if (node is Polygon2D polygon)
		{
			if (polygon.HasMeta("flicker_lamp"))
			{
				Color color = polygon.Color;
				color.A = 0.16f + (float)Math.Abs(Math.Sin(t * 21.0)) * 0.18f;
				polygon.Color = color;
			}
			if (polygon.HasMeta("world_stutter")) polygon.Scale = new Vector2(1.0f + (float)Math.Sin(t * 14.0 + polygon.Position.X) * 0.12f, 1.0f);
			if (polygon.HasMeta("trigger_pixel_edge")) polygon.Visible = ((int)(t * 15.0 + polygon.Position.X * 0.1f) % 7) != 0;
			if (polygon.HasMeta("train_light_core"))
			{
				float pulse = 1.0f + (float)Math.Abs(Math.Sin(t * 2.8)) * 0.18f;
				polygon.Scale = new Vector2(pulse, pulse);
			}
			if (polygon.HasMeta("train_beam"))
			{
				Color color = polygon.Color;
				color.A = 0.14f + (float)Math.Abs(Math.Sin(t * 1.8)) * 0.14f;
				polygon.Color = color;
			}
		}
		if (node is Line2D line)
		{
			if (line.HasMeta("falling_rain")) line.Position = new Vector2(0, (float)((t * 260.0 + line.GetInstanceId() % 97) % 620) - 310);
			if (line.HasMeta("reverse_rain")) line.Position = new Vector2(0, -(float)((t * 110.0 + line.GetInstanceId() % 83) % 300));
			if (line.HasMeta("train_light_streak")) line.Width = 3.0f + (float)Math.Abs(Math.Sin(t * 9.0)) * 4.0f;
		}
		if (node is Label label && label.HasMeta("glitch_digits"))
		{
			Color color = label.Modulate;
			color.A = ((Time.GetTicksMsec() / 120) % 2 == 0) ? 0.95f : 0.55f;
			label.Modulate = color;
		}
		foreach (var child in node.GetChildren()) AnimateNodeTree(child);
	}
}
