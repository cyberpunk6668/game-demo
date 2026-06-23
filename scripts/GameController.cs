using Godot;
using System;
using System.Collections.Generic;

public partial class GameController : Node3D
{
	private Node3D _worldRoot = null!;
	private Camera3D _camera = null!;
	private WorldEnvironment _worldEnvironment = null!;
	private DirectionalLight3D _coldDirectionalLight = null!;
	private CanvasLayer _canvas = null!;
	private Control _hudRoot = null!;
	private Label _titleLabel = null!;
	private Label _bodyLabel = null!;
	private Label _phaseLabel = null!;
	private Label _memoryLabel = null!;
	private VBoxContainer _actions = null!;
	private ColorRect _glitchOverlay = null!;
	private AudioStreamPlayer _hum = null!;
	private readonly List<Node> _phaseNodes = new();
	private readonly List<Node> _hudEffectNodes = new();
	private double _glitchTime;

	public override void _Ready()
	{
		EnsureTimeManager();
		CreateBaseWorld();
		CreateHud();
		CreateAmbientAudio();
		RenderPhase();
	}

	public override void _Process(double delta)
	{
		AnimateGlitch(delta);
		AnimateScene(delta);
		AnimateHudEffects(delta);
	}

	public override void _ExitTree()
	{
		if (_hum != null)
		{
			_hum.Stop();
			_hum.Stream = null;
		}
	}

	private void EnsureTimeManager()
	{
		if (TimeManager.Instance == null)
		{
			AddChild(new TimeManager { Name = "TimeManager" });
		}
	}

	private TimeManager Timeline => TimeManager.Instance!;

	private void CreateBaseWorld()
	{
		_worldRoot = new Node3D { Name = "WorldRoot" };
		AddChild(_worldRoot);

		_camera = new Camera3D
		{
			Name = "Camera3D",
			Position = new Vector3(0, 3.2f, 8.5f),
			RotationDegrees = new Vector3(-18, 0, 0),
			Fov = 58,
			Near = 0.04f,
			Far = 80.0f,
			Current = true
		};
		AddChild(_camera);

		_worldEnvironment = new WorldEnvironment
		{
			Name = "WorldEnvironment",
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.02f, 0.025f, 0.035f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.12f, 0.14f, 0.18f),
				AmbientLightEnergy = 0.75f,
				FogEnabled = true,
				FogLightColor = new Color(0.08f, 0.09f, 0.12f),
				FogDensity = 0.045f
			}
		};
		AddChild(_worldEnvironment);

		_coldDirectionalLight = new DirectionalLight3D
		{
			Name = "ColdMoonLight",
			RotationDegrees = new Vector3(-45, 35, 0),
			LightColor = new Color(0.55f, 0.65f, 1.0f),
			LightEnergy = 0.65f,
			ShadowEnabled = true,
			ShadowOpacity = 0.45f,
			LightVolumetricFogEnergy = 0.35f
		};
		AddChild(_coldDirectionalLight);
	}

	private void CreateHud()
	{
		_canvas = new CanvasLayer { Name = "HUD" };
		AddChild(_canvas);

		_hudRoot = new Control
		{
			Name = "HudRoot",
			AnchorRight = 1,
			AnchorBottom = 1
		};
		_canvas.AddChild(_hudRoot);

		var panel = new PanelContainer
		{
			Name = "StoryPanel",
			AnchorLeft = 0.03f,
			AnchorTop = 0.04f,
			AnchorRight = 0.45f,
			AnchorBottom = 0.96f,
			OffsetLeft = 0,
			OffsetTop = 0,
			OffsetRight = 0,
			OffsetBottom = 0
		};
		_hudRoot.AddChild(panel);

		var margin = new MarginContainer
		{
			Name = "Margin",
			ThemeTypeVariation = "MarginContainer"
		};
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
			Name = "GlitchOverlay",
			AnchorRight = 1,
			AnchorBottom = 1,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.3f, 0.85f, 1.0f, 0.0f)
		};
		_hudRoot.AddChild(_glitchOverlay);
	}

	private void CreateAmbientAudio()
	{
		_hum = new AudioStreamPlayer
		{
			Name = "SuffocatingSilenceAudioAnchor",
			Autoplay = false,
			VolumeDb = -80
		};
		AddChild(_hum);
	}

	private Label CreateLabel(string name, int size, Color color, bool bold)
	{
		var label = new Label
		{
			Name = name,
			Text = name,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
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
				RenderCinematicApartmentOpening();
				SetStory(
					"第一场：压抑单间公寓",
					"Cinematic Opening / 乱码日历与打翻的杯子",
					"冷青色的室内光压在墙上，唯一的暖色来自忽明忽暗的台灯。木桌上的陶瓷杯已经倒下，干涸水痕像被时间擦除过的证据。墙上的电子钟/日历只剩乱码、撕裂线和无法读取的日期。",
					("观察打翻的杯子", () => MarkAndRefresh("CupKnocked", "杯沿是干的，水痕却像刚刚停止流动。")),
					("靠近乱码日历", () => MarkAndRefresh("DiaryRead", "数字在 88:?? 与一串错误日期之间撕裂，像有人把时间揉皱后钉在墙上。")),
					("进入时间回环", () => ForceJump(TimePhase.AccidentNightFirst, "房间里的地铁低鸣突然贴近耳膜。你被抛向事故当晚的月台。")));
				break;

			case TimePhase.AccidentNightFirst:
				RenderPlatform(firstVisit: true, finalVisit: false, accepted: false);
				SetStory(
					"第一跃：事故当晚",
					"月台 / 强制干预",
					"雨声像坏掉的磁带一样循环。她站在黄线外侧，列车灯从雾里刺出来。你以为只要跑得够快、说对一句话，就能把这一晚扳回正轨。",
					("冲过去拉住她", () => ForceJump(TimePhase.ApartmentBefore, "你扑向边缘。玻璃一样的裂纹爬满视野，世界把你弹回事故前三天。")),
					("大喊她的名字", () => ForceJump(TimePhase.ApartmentBefore, "她回头了，可列车声吞没了回答。时间线崩裂，日期倒卷。")));
				break;

			case TimePhase.ApartmentBefore:
				RenderApartment(beforeAccident: true);
				SetStory(
					"第二跃：事故前三天",
					"公寓 / 搜查与藏钥匙",
					"房间还保留着生活的温度。电子钟正常跳动，杯子里的水没有干，日记摊在桌角。你开始搜寻所谓的“线索”，像是在审问一段本不该被改写的过去。",
					("阅读日记碎片", () => MarkAndRefresh("DiaryRead", "日记里没有谜底，只有一句反复涂黑的话：‘别再替我决定。’")),
					("藏起门口钥匙", () => MarkAndRefresh("KeyHidden", "钥匙被你塞进沙发缝。下一秒，门锁自己响了一声，像在嘲笑。")),
					("打翻水杯", () => MarkAndRefresh("CupKnocked", "水杯倒下，水迹却逆着桌面爬回杯口。")),
					("强行改变过去", () => ForceJump(TimePhase.ApartmentAfter, "你越想修正，房间越像旧录像一样卡顿。画面被撕开，事故已经发生后一周。")));
				break;

			case TimePhase.ApartmentAfter:
				RenderApartment(beforeAccident: false);
				SetStory(
					"第三跃：事故后一周",
					"公寓 / 空房间与记忆手术",
					"公寓空得不自然。电子钟显示乱码，镜子里的你额角有一道细小伤疤。药瓶标签写着：‘记忆干预后按需服用。’你终于意识到，被困住的不只是时间。",
					("照镜子看伤疤", () => MarkAndRefresh("MirrorChecked", "伤疤发烫。你想起自己主动要求忘掉这一晚，又一次主动把它挖开。")),
					("服药维持理智", () => MarkAndRefresh("MedicineTaken", "苦味压下耳鸣，远处地铁的轰鸣却更清楚了。")),
					("推开公寓大门", () => ForceJump(TimePhase.FinalPlatform, "门外不是走廊。铁轨、雨、黄线和刺眼车灯一起涌进来。")));
				break;

			case TimePhase.FinalPlatform:
				RenderPlatform(firstVisit: false, finalVisit: true, accepted: false);
				SetStory(
					"第四跃：事故当晚",
					"月台 / 最后的内心抉择",
					"一切回到原点，但这一次你知道：循环并不是给你一次完美操作的机会，而是在逼你承认有些遗憾不能被胜利条件覆盖。她仍站在边缘。你的手在发抖。",
					("伸手拉她", () => RestartLoop()),
					("闭眼转身", () => AcceptEnding()));
				break;

			case TimePhase.AcceptedEnding:
				RenderPlatform(firstVisit: false, finalVisit: false, accepted: true);
				SetStory(
					"通关：时间继续向前",
					"清空的月台 / 接纳遗憾",
					"列车驶过。没有乱码，没有碎裂，也没有新的跳跃。雨停在半空，又终于落下。月台空了，但时间第一次没有回头。",
					("重新体验循环", () => { Timeline.ResetTimeline(); RenderPhase(); }));
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
			var button = new Button
			{
				Text = action.Text,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				FocusMode = Control.FocusModeEnum.None
			};
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
		string flags =
			$"日记:{Flag("DiaryRead")}  钥匙:{Flag("KeyHidden")}  水杯:{Flag("CupKnocked")}\n" +
			$"镜子:{Flag("MirrorChecked")}  药:{Flag("MedicineTaken")}  门:{Flag("DoorOpened")}";

		_memoryLabel.Text = "跨时间物品状态\n" + flags + "\n\n记忆残响\n" +
			(Timeline.MemoryLog.Count == 0 ? "还没有可相信的记忆。" : string.Join("\n", Timeline.MemoryLog));
	}

	private string Flag(string key) => Timeline.GetState(key) ? "已改变" : "未改变";

	private void RenderApartment(bool beforeAccident)
	{
		ConfigureApartmentCamera(beforeAccident);
		var room = AddPhaseNode(new Node3D { Name = beforeAccident ? "Apartment_Before" : "Apartment_After" });
		AddBox(room, "Floor", new Vector3(0, -0.06f, 0), new Vector3(7, 0.1f, 6), beforeAccident ? new Color(0.18f, 0.16f, 0.14f) : new Color(0.09f, 0.1f, 0.12f));
		AddBox(room, "BackWall", new Vector3(0, 1.75f, -3), new Vector3(7, 3.5f, 0.12f), new Color(0.16f, 0.17f, 0.2f));
		AddBox(room, "LeftWall", new Vector3(-3.5f, 1.75f, 0), new Vector3(0.12f, 3.5f, 6), new Color(0.12f, 0.13f, 0.16f));
		AddBox(room, "Desk", new Vector3(-1.35f, 0.55f, -1.6f), new Vector3(1.8f, 0.2f, 0.9f), new Color(0.24f, 0.18f, 0.13f));
		AddBox(room, "Sofa", new Vector3(1.65f, 0.35f, -1.2f), new Vector3(1.9f, 0.7f, 0.9f), new Color(0.14f, 0.18f, 0.24f));

		var cupTilt = Timeline.GetState("CupKnocked") ? -70 : 0;
		var cup = AddBox(room, "Cup", new Vector3(-1.8f, 0.78f, -1.55f), new Vector3(0.22f, 0.36f, 0.22f), Timeline.GetState("CupKnocked") ? new Color(0.45f, 0.55f, 0.6f) : Colors.White);
		cup.RotationDegrees = new Vector3(0, 0, cupTilt);
		if (Timeline.GetState("CupKnocked"))
		{
			AddBox(room, "DryWaterStain", new Vector3(-1.45f, 0.665f, -1.55f), new Vector3(0.85f, 0.015f, 0.38f), new Color(0.32f, 0.42f, 0.48f, 0.75f));
		}

		AddBox(room, "Door", new Vector3(3.0f, 1.1f, -2.93f), new Vector3(0.8f, 2.2f, 0.08f), Timeline.CurrentPhase == TimePhase.ApartmentAfter ? new Color(0.02f, 0.03f, 0.06f) : new Color(0.22f, 0.16f, 0.1f));
		AddBox(room, "Mirror", new Vector3(-3.42f, 1.45f, -1.0f), new Vector3(0.04f, 1.15f, 0.75f), Timeline.GetState("MirrorChecked") ? new Color(0.55f, 0.75f, 0.9f) : new Color(0.25f, 0.35f, 0.42f));

		var lamp = new OmniLight3D
		{
			Name = "FlickeringLamp",
			Position = new Vector3(-1.8f, 1.45f, -1.5f),
			LightColor = beforeAccident ? new Color(1.0f, 0.76f, 0.45f) : new Color(0.45f, 0.62f, 1.0f),
			LightEnergy = beforeAccident ? 2.3f : 1.1f,
			OmniRange = 5.0f
		};
		room.AddChild(lamp);

		AddFloatingText(room, beforeAccident ? "03 DAYS BEFORE" : "ERROR: +7 DAYS", new Vector3(0, 2.35f, -2.88f), beforeAccident ? new Color(0.9f, 0.8f, 0.55f) : new Color(1.0f, 0.25f, 0.35f));
	}

	private void RenderCinematicApartmentOpening()
	{
		ConfigureOpeningApartmentCamera();
		Timeline.MarkState("CupKnocked", true);

		var room = AddPhaseNode(new Node3D { Name = "Cinematic_Opening_Apartment" });
		AddBox(room, "ClaustrophobicFloor_DarkWood", new Vector3(0, -0.055f, 0), new Vector3(7.6f, 0.1f, 6.2f), new Color(0.075f, 0.065f, 0.055f));
		AddBox(room, "OppressiveBackWall_CyanStained", new Vector3(0, 1.25f, -3.05f), new Vector3(7.6f, 2.6f, 0.12f), new Color(0.055f, 0.085f, 0.095f));
		AddBox(room, "LeftWall_Close", new Vector3(-3.8f, 1.25f, 0), new Vector3(0.12f, 2.6f, 6.2f), new Color(0.045f, 0.065f, 0.075f));
		AddBox(room, "RightWall_Close", new Vector3(3.8f, 1.25f, 0), new Vector3(0.12f, 2.6f, 6.2f), new Color(0.04f, 0.055f, 0.065f));
		AddBox(room, "LowCeiling", new Vector3(0, 2.52f, 0), new Vector3(7.6f, 0.1f, 6.2f), new Color(0.035f, 0.045f, 0.055f));
		AddCinematicWallAndFloorDetail(room);

		AddDeskFocus(room);
		AddGlitchedCalendar(room);
		AddBedAndClutter(room);
		AddOverturnedChair(room);
		AddTakeoutPile(room);
		AddWiltedPlant(room);
		AddFloatingGlassCracks(room);
		AddChromaticAberrationAccents(room);
		AddOpeningLights(room);
	}

	private void ConfigureOpeningApartmentCamera()
	{
		_camera.Position = new Vector3(-0.2f, 1.34f, 4.55f);
		_camera.Fov = 55;
		_camera.Attributes = new CameraAttributesPractical
		{
			DofBlurFarEnabled = true,
			DofBlurFarDistance = 7.65f,
			DofBlurFarTransition = 1.15f,
			DofBlurNearEnabled = true,
			DofBlurNearDistance = 4.45f,
			DofBlurNearTransition = 0.85f,
			DofBlurAmount = 0.17f
		};
		_camera.LookAt(new Vector3(-0.72f, 0.82f, -1.92f), Vector3.Up);

		_coldDirectionalLight.LightEnergy = 0.1f;
		_coldDirectionalLight.LightColor = new Color(0.1f, 0.55f, 0.62f);
		_coldDirectionalLight.LightVolumetricFogEnergy = 1.2f;

		var environment = _worldEnvironment.Environment;
		environment.BackgroundColor = new Color(0.008f, 0.012f, 0.018f);
		environment.AmbientLightColor = new Color(0.08f, 0.22f, 0.25f);
		environment.AmbientLightEnergy = 0.48f;
		environment.FogEnabled = true;
		environment.FogLightColor = new Color(0.05f, 0.16f, 0.18f);
		environment.FogDensity = 0.075f;
		environment.VolumetricFogEnabled = true;
		environment.VolumetricFogDensity = 0.028f;
		environment.VolumetricFogAlbedo = new Color(0.1f, 0.35f, 0.38f);
		environment.TonemapMode = Godot.Environment.ToneMapper.Aces;
		environment.TonemapExposure = 0.82f;
		environment.TonemapWhite = 6.5f;
		environment.SsaoEnabled = true;
		environment.SsaoRadius = 2.2f;
		environment.SsaoIntensity = 2.4f;
		environment.SsaoPower = 1.65f;
		environment.SsilEnabled = true;
		environment.SsilRadius = 2.0f;
		environment.SsilIntensity = 0.55f;
		environment.GlowEnabled = true;
		environment.GlowIntensity = 0.22f;
		environment.GlowStrength = 0.65f;
		environment.GlowBloom = 0.08f;
		environment.AdjustmentEnabled = true;
		environment.AdjustmentBrightness = 0.88f;
		environment.AdjustmentContrast = 1.32f;
		environment.AdjustmentSaturation = 0.68f;
	}

	private void ConfigureApartmentCamera(bool beforeAccident)
	{
		_camera.Attributes = null;
		_camera.Position = new Vector3(0, 3.2f, 8.5f);
		_camera.Fov = 58;
		_camera.LookAt(new Vector3(0, 0.85f, -1.8f), Vector3.Up);

		_coldDirectionalLight.LightEnergy = 0.65f;
		_coldDirectionalLight.LightColor = new Color(0.55f, 0.65f, 1.0f);
		_coldDirectionalLight.LightVolumetricFogEnergy = 0.35f;

		var environment = _worldEnvironment.Environment;
		environment.BackgroundColor = new Color(0.02f, 0.025f, 0.035f);
		environment.AmbientLightColor = beforeAccident ? new Color(0.13f, 0.13f, 0.16f) : new Color(0.08f, 0.12f, 0.18f);
		environment.AmbientLightEnergy = beforeAccident ? 0.78f : 0.58f;
		environment.FogEnabled = true;
		environment.FogLightColor = new Color(0.08f, 0.09f, 0.12f);
		environment.FogDensity = beforeAccident ? 0.038f : 0.055f;
		environment.VolumetricFogEnabled = false;
		environment.TonemapMode = Godot.Environment.ToneMapper.Filmic;
		environment.TonemapExposure = 1.0f;
		environment.TonemapWhite = 1.0f;
		environment.SsaoEnabled = false;
		environment.SsilEnabled = false;
		environment.GlowEnabled = false;
		environment.AdjustmentEnabled = false;
	}

	private void AddCinematicWallAndFloorDetail(Node3D room)
	{
		AddBox(room, "BackWallBaseboard", new Vector3(0, 0.16f, -2.955f), new Vector3(7.4f, 0.13f, 0.04f), new Color(0.025f, 0.03f, 0.032f));
		AddBox(room, "LeftWallBaseboard", new Vector3(-3.725f, 0.16f, 0), new Vector3(0.04f, 0.13f, 5.9f), new Color(0.025f, 0.03f, 0.032f));
		AddBox(room, "RightWallBaseboard", new Vector3(3.725f, 0.16f, 0), new Vector3(0.04f, 0.13f, 5.9f), new Color(0.02f, 0.026f, 0.03f));

		for (int i = 0; i < 16; i++)
		{
			float x = -3.25f + (i % 8) * 0.9f;
			float z = -2.45f + (i / 8) * 1.25f;
			var grain = AddBox(room, "DarkWoodGrain_ProceduralStrip", new Vector3(x, 0.004f, z), new Vector3(0.62f, 0.012f, 0.018f), new Color(0.12f, 0.085f, 0.05f, 0.48f));
			grain.RotationDegrees = new Vector3(0, 5 + i * 13, 0);
		}

		for (int i = 0; i < 9; i++)
		{
			var scratch = AddBox(room, "TimeErasedStruggleScuff", new Vector3(-2.7f + i * 0.58f, 0.025f, -0.1f + (i % 3) * 0.36f), new Vector3(0.46f, 0.016f, 0.028f), new Color(0.42f, 0.44f, 0.4f, 0.42f));
			scratch.RotationDegrees = new Vector3(0, -32 + i * 11, 0);
		}

		for (int i = 0; i < 7; i++)
		{
			var wallSmear = AddBox(room, "FaintWallSmear_ErasedByTime", new Vector3(-2.8f + i * 0.72f, 1.0f + (i % 2) * 0.22f, -2.925f), new Vector3(0.34f, 0.028f, 0.018f), new Color(0.13f, 0.18f, 0.18f, 0.55f));
			wallSmear.RotationDegrees = new Vector3(0, 0, -20 + i * 9);
		}
	}

	private void AddDeskFocus(Node3D room)
	{
		AddBox(room, "HeavyWoodenDesk", new Vector3(-0.75f, 0.52f, -1.7f), new Vector3(2.65f, 0.18f, 1.05f), new Color(0.2f, 0.125f, 0.07f));
		AddBox(room, "DeskFrontShadow", new Vector3(-0.75f, 0.24f, -1.15f), new Vector3(2.65f, 0.55f, 0.08f), new Color(0.065f, 0.04f, 0.025f));
		AddBox(room, "DeskLeftLeg", new Vector3(-1.9f, 0.22f, -2.05f), new Vector3(0.12f, 0.55f, 0.12f), new Color(0.13f, 0.08f, 0.045f));
		AddBox(room, "DeskRightLeg", new Vector3(0.35f, 0.22f, -2.05f), new Vector3(0.12f, 0.55f, 0.12f), new Color(0.13f, 0.08f, 0.045f));

		for (int i = 0; i < 10; i++)
		{
			var grain = AddBox(room, "DeskWoodGrain_HighDetail", new Vector3(-1.78f + i * 0.23f, 0.625f, -1.68f + (i % 2) * 0.28f), new Vector3(0.18f, 0.014f, 0.018f), new Color(0.31f, 0.2f, 0.11f, 0.5f));
			grain.RotationDegrees = new Vector3(0, -8 + i * 4, 0);
		}

		var mug = AddCylinder(room, "TippedCeramicMug_DryStained", new Vector3(-1.08f, 0.725f, -1.62f), 0.15f, 0.42f, new Color(0.78f, 0.78f, 0.72f));
		mug.RotationDegrees = new Vector3(0, 0, 92);
		var mugMouth = AddCylinder(room, "DarkMugOpening", new Vector3(-0.86f, 0.718f, -1.62f), 0.125f, 0.018f, new Color(0.08f, 0.065f, 0.055f));
		mugMouth.RotationDegrees = new Vector3(0, 0, 92);
		AddBox(room, "MugHandle", new Vector3(-1.08f, 0.73f, -1.37f), new Vector3(0.08f, 0.22f, 0.05f), new Color(0.7f, 0.7f, 0.66f));
		var mugStain = AddBox(room, "MugSurfaceDriedWaterStain", new Vector3(-1.12f, 0.82f, -1.51f), new Vector3(0.18f, 0.018f, 0.08f), new Color(0.42f, 0.38f, 0.3f, 0.82f));
		mugStain.RotationDegrees = new Vector3(0, 0, 92);
		AddCylinder(room, "DryWaterRingOuter", new Vector3(-0.7f, 0.626f, -1.62f), 0.34f, 0.012f, new Color(0.26f, 0.19f, 0.13f));
		AddCylinder(room, "DryWaterRingInner_WoodShowing", new Vector3(-0.7f, 0.634f, -1.62f), 0.24f, 0.014f, new Color(0.2f, 0.125f, 0.07f));
		AddBox(room, "DriedWaterStreak", new Vector3(-0.36f, 0.64f, -1.67f), new Vector3(0.52f, 0.012f, 0.08f), new Color(0.33f, 0.28f, 0.21f, 0.78f));
		AddBox(room, "SecondaryDryWaterRing", new Vector3(-0.12f, 0.642f, -1.54f), new Vector3(0.42f, 0.012f, 0.18f), new Color(0.22f, 0.19f, 0.14f, 0.54f));

		for (int i = 0; i < 6; i++)
		{
			var page = AddBox(room, "TornDiaryPage_OnDesk", new Vector3(-1.65f + i * 0.22f, 0.642f, -2.05f + (i % 2) * 0.16f), new Vector3(0.28f, 0.01f, 0.2f), new Color(0.77f, 0.73f, 0.62f));
			page.RotationDegrees = new Vector3(0, 15 + i * 17, 0);
			AddBox(room, "BlackDiaryInkLine", page.Position + new Vector3(0, 0.011f, 0), new Vector3(0.2f, 0.008f, 0.018f), new Color(0.025f, 0.02f, 0.018f));
		}
	}

	private void AddGlitchedCalendar(Node3D room)
	{
		AddBox(room, "DigitalClockCalendar_WallMountFrame", new Vector3(0.62f, 1.62f, -2.99f), new Vector3(1.52f, 0.72f, 0.06f), new Color(0.018f, 0.02f, 0.023f));
		AddBox(room, "DigitalClockCalendar_BlackScreen", new Vector3(0.62f, 1.62f, -2.97f), new Vector3(1.35f, 0.56f, 0.045f), new Color(0.005f, 0.008f, 0.012f));
		AddEmissiveBox(room, "DigitalClockCalendar_CyanBleed", new Vector3(0.58f, 1.62f, -2.94f), new Vector3(1.39f, 0.6f, 0.018f), new Color(0.0f, 0.28f, 0.32f, 0.42f), 0.7f);

		for (int i = 0; i < 9; i++)
		{
			float y = 1.38f + i * 0.06f;
			var scanline = AddEmissiveBox(room, "CalendarTearingScanline", new Vector3(0.62f + ((i % 3) - 1) * 0.05f, y, -2.91f), new Vector3(1.24f - i * 0.035f, 0.012f, 0.018f), new Color(0.0f, 0.62f, 0.68f, 0.68f), 1.1f);
			scanline.SetMeta("calendar_scanline", true);
		}

		var whiteDigits = AddWallLabel(room, "88:??\n2█/##/??", new Vector3(0.62f, 1.65f, -2.88f), new Color(0.75f, 1.0f, 0.96f), 0.012f);
		var redDigits = AddWallLabel(room, "88:??\n2█/##/??", new Vector3(0.59f, 1.65f, -2.875f), new Color(1.0f, 0.08f, 0.12f), 0.012f);
		var blueDigits = AddWallLabel(room, "88:??\n2█/##/??", new Vector3(0.66f, 1.65f, -2.87f), new Color(0.05f, 0.45f, 1.0f), 0.012f);
		whiteDigits.SetMeta("glitch_digits", true);
		redDigits.SetMeta("glitch_digits", true);
		blueDigits.SetMeta("glitch_digits", true);
	}

	private void AddBedAndClutter(Node3D room)
	{
		AddBox(room, "MessyBedFrame", new Vector3(2.25f, 0.28f, -0.9f), new Vector3(2.2f, 0.32f, 1.7f), new Color(0.12f, 0.08f, 0.055f));
		AddBox(room, "StainedMattress", new Vector3(2.25f, 0.52f, -0.9f), new Vector3(2.08f, 0.22f, 1.58f), new Color(0.34f, 0.35f, 0.34f));
		var blanket = AddBox(room, "TwistedUnmadeBlanket", new Vector3(2.05f, 0.72f, -0.65f), new Vector3(1.55f, 0.16f, 0.9f), new Color(0.1f, 0.16f, 0.22f));
		blanket.RotationDegrees = new Vector3(0, -18, 5);
		AddBox(room, "CollapsedPillow", new Vector3(1.45f, 0.74f, -1.55f), new Vector3(0.62f, 0.16f, 0.34f), new Color(0.58f, 0.58f, 0.55f));

		for (int i = 0; i < 12; i++)
		{
			float x = -2.6f + (i % 4) * 0.55f;
			float z = 0.25f + (i / 4) * 0.52f;
			var page = AddBox(room, "ScatteredTornDiaryPage", new Vector3(x, 0.012f, z), new Vector3(0.32f, 0.012f, 0.22f), new Color(0.76f, 0.72f, 0.62f));
			page.RotationDegrees = new Vector3(0, i * 31, 0);
			AddBox(room, "DiaryPageTornInk", page.Position + new Vector3(0, 0.012f, 0.02f), new Vector3(0.2f, 0.008f, 0.014f), new Color(0.03f, 0.025f, 0.02f, 0.8f));
		}
	}

	private void AddOverturnedChair(Node3D room)
	{
		var seat = AddBox(room, "OverturnedChairSeat", new Vector3(-2.15f, 0.31f, -0.45f), new Vector3(0.75f, 0.12f, 0.65f), new Color(0.15f, 0.09f, 0.045f));
		seat.RotationDegrees = new Vector3(15, 0, 78);
		for (int i = 0; i < 4; i++)
		{
			float sx = (i % 2 == 0) ? -0.28f : 0.28f;
			float sz = (i < 2) ? -0.23f : 0.23f;
			var leg = AddBox(room, "OverturnedChairLeg", new Vector3(-2.15f + sx, 0.33f, -0.45f + sz), new Vector3(0.06f, 0.72f, 0.06f), new Color(0.12f, 0.07f, 0.035f));
			leg.RotationDegrees = new Vector3(18 + i * 7, 0, 68);
		}
		var back = AddBox(room, "OverturnedChairBack", new Vector3(-2.48f, 0.52f, -0.68f), new Vector3(0.08f, 0.92f, 0.72f), new Color(0.13f, 0.075f, 0.04f));
		back.RotationDegrees = new Vector3(10, 0, 74);
	}

	private void AddTakeoutPile(Node3D room)
	{
		for (int i = 0; i < 7; i++)
		{
			float layer = i / 3;
			var box = AddBox(room, "EmptyTakeoutBoxPile", new Vector3(2.9f + (i % 3) * 0.22f, 0.09f + layer * 0.13f, 1.15f + (i % 2) * 0.18f), new Vector3(0.42f, 0.14f, 0.34f), new Color(0.48f, 0.38f, 0.25f));
			box.RotationDegrees = new Vector3(0, i * 13, (i % 2) * 8);
			AddBox(room, "TakeoutGreaseMark", box.Position + new Vector3(0, 0.075f, 0), new Vector3(0.24f, 0.012f, 0.16f), new Color(0.27f, 0.18f, 0.09f));
		}
	}

	private void AddWiltedPlant(Node3D room)
	{
		var pot = AddCylinder(room, "CrackedPlantPot", new Vector3(-3.05f, 0.22f, -2.25f), 0.24f, 0.42f, new Color(0.25f, 0.12f, 0.08f));
		pot.RotationDegrees = new Vector3(0, 0, -7);
		AddBox(room, "PlantPotCrack", new Vector3(-3.0f, 0.33f, -2.02f), new Vector3(0.022f, 0.32f, 0.018f), new Color(0.02f, 0.015f, 0.012f));
		for (int i = 0; i < 5; i++)
		{
			var stem = AddBox(room, "WiltedPlantStem", new Vector3(-3.05f + (i - 2) * 0.055f, 0.58f, -2.23f), new Vector3(0.025f, 0.68f, 0.025f), new Color(0.11f, 0.22f, 0.09f));
			stem.RotationDegrees = new Vector3(0, 0, -28 + i * 14);
			var leaf = AddBox(room, "WiltedPlantLeaf", stem.Position + new Vector3(0.05f * (i - 2), 0.28f, 0.02f), new Vector3(0.22f, 0.035f, 0.1f), new Color(0.08f, 0.18f, 0.07f));
			leaf.RotationDegrees = new Vector3(0, i * 22, -35 + i * 11);
		}
	}

	private void AddFloatingGlassCracks(Node3D room)
	{
		Vector3[] anchors =
		{
			new(-1.75f, 1.42f, 0.38f),
			new(-0.28f, 1.58f, 0.06f),
			new(1.18f, 1.28f, -0.35f),
			new(0.92f, 1.84f, -1.1f)
		};

		for (int i = 0; i < anchors.Length; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				var crack = AddEmissiveBox(room, "FloatingBrokenGlassCrack", anchors[i] + new Vector3(j * 0.08f, j * 0.045f, -j * 0.015f), new Vector3(0.62f - j * 0.08f, 0.016f, 0.016f), new Color(0.58f, 0.93f, 1.0f, 0.42f), 0.35f);
				crack.RotationDegrees = new Vector3(0, 0, -45 + i * 24 + j * 31);
				crack.SetMeta("glass_crack", true);
			}
		}
	}

	private void AddChromaticAberrationAccents(Node3D room)
	{
		AddEmissiveBox(room, "RGBShift_CalendarRedEdge", new Vector3(-0.08f, 1.92f, -2.88f), new Vector3(0.06f, 0.62f, 0.018f), new Color(1.0f, 0.05f, 0.08f, 0.58f), 0.8f);
		AddEmissiveBox(room, "RGBShift_CalendarCyanEdge", new Vector3(1.34f, 1.32f, -2.87f), new Vector3(0.06f, 0.62f, 0.018f), new Color(0.02f, 0.75f, 1.0f, 0.58f), 0.8f);
		AddEmissiveBox(room, "RGBShift_MugRedGhost", new Vector3(-1.13f, 0.75f, -1.23f), new Vector3(0.42f, 0.035f, 0.035f), new Color(1.0f, 0.05f, 0.08f, 0.45f), 0.45f);
		AddEmissiveBox(room, "RGBShift_MugCyanGhost", new Vector3(-1.03f, 0.7f, -1.98f), new Vector3(0.42f, 0.035f, 0.035f), new Color(0.0f, 0.65f, 1.0f, 0.45f), 0.45f);
	}

	private void AddOpeningLights(Node3D room)
	{
		AddBox(room, "DeskLampBase", new Vector3(-1.92f, 0.72f, -1.92f), new Vector3(0.28f, 0.08f, 0.28f), new Color(0.025f, 0.02f, 0.015f));
		AddBox(room, "DeskLampStem", new Vector3(-1.92f, 1.02f, -1.92f), new Vector3(0.055f, 0.62f, 0.055f), new Color(0.035f, 0.03f, 0.025f));
		AddBox(room, "DeskLampShade", new Vector3(-1.92f, 1.32f, -1.92f), new Vector3(0.5f, 0.24f, 0.38f), new Color(0.85f, 0.42f, 0.12f));

		var warmLamp = new OmniLight3D
		{
			Name = "FlickeringDeskLamp",
			Position = new Vector3(-1.92f, 1.28f, -1.82f),
			LightColor = new Color(1.0f, 0.48f, 0.16f),
			LightEnergy = 3.6f,
			OmniRange = 4.7f,
			ShadowEnabled = true,
			ShadowOpacity = 0.85f,
			LightVolumetricFogEnergy = 3.0f
		};
		warmLamp.SetMeta("flicker_lamp", true);
		room.AddChild(warmLamp);

		var coldFill = new OmniLight3D
		{
			Name = "SicklyCyanAmbientFill",
			Position = new Vector3(1.8f, 1.9f, 1.6f),
			LightColor = new Color(0.08f, 0.55f, 0.62f),
			LightEnergy = 0.38f,
			OmniRange = 6.0f,
			ShadowEnabled = true,
			ShadowOpacity = 0.22f,
			LightVolumetricFogEnergy = 1.3f
		};
		room.AddChild(coldFill);
	}

	private MeshInstance3D AddCylinder(Node parent, string name, Vector3 position, float radius, float height, Color color)
	{
		var mesh = new CylinderMesh
		{
			TopRadius = radius,
			BottomRadius = radius,
			Height = height,
			RadialSegments = 32
		};
		mesh.Material = CreateMaterial(color, 0.88f, 0.0f);

		var instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			Position = position
		};
		parent.AddChild(instance);
		return instance;
	}

	private Label3D AddWallLabel(Node3D parent, string text, Vector3 position, Color color, float pixelSize)
	{
		var label = new Label3D
		{
			Name = "GlitchedCalendarDigits",
			Text = text,
			Position = position,
			PixelSize = pixelSize,
			Modulate = color,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
		};
		parent.AddChild(label);
		return label;
	}

	private void RenderPlatform(bool firstVisit, bool finalVisit, bool accepted)
	{
		ConfigurePlatformCamera(accepted);
		if (!accepted)
		{
			RenderTemporalChaosHud(finalVisit);
		}

		var platform = AddPhaseNode(new Node3D { Name = accepted ? "Empty_Platform_AfterAcceptance" : "AccidentNight_SubwayPlatform_SecondScene" });
		AddSubwayPlatformSlice(platform, accepted);

		if (accepted)
		{
			AddFloatingText(platform, "时间恢复向前", new Vector3(0, 1.55f, -1.15f), new Color(0.85f, 0.95f, 1.0f));
			return;
		}

		AddSubwayRainParticles(platform);
		AddWarningTriggerZone(platform);
		AddDynamicTrainLight(platform, firstVisit, finalVisit);
		AddFluorescentPlatformLights(platform);
		AddDiscardedUmbrella(platform);
		AddWetFloorReflections(platform);
		AddTemporalChaosWorldGlitches(platform);
		AddBox(platform, "HerSilhouette_AtYellowLine", new Vector3(1.35f, 0.68f, -0.96f), new Vector3(0.26f, 1.35f, 0.18f), finalVisit ? new Color(0.9f, 0.86f, 0.78f, 0.82f) : new Color(0.55f, 0.62f, 0.74f, 0.72f));
	}

	private void AddSubwayPlatformSlice(Node3D platform, bool accepted)
	{
		AddPbrBox(platform, "FiveMeterPlatformSlice_WetConcrete", new Vector3(0, -0.055f, 0.0f), new Vector3(5.0f, 0.1f, 2.05f), accepted ? new Color(0.16f, 0.17f, 0.18f) : new Color(0.055f, 0.063f, 0.072f), 0.24f, 0.0f);
		AddBox(platform, "PlatformEdgeVerticalDrop", new Vector3(0, -0.22f, -1.12f), new Vector3(5.0f, 0.34f, 0.16f), accepted ? new Color(0.11f, 0.12f, 0.13f) : new Color(0.025f, 0.03f, 0.036f));
		AddPbrBox(platform, "TrackBed_BlackRainwater", new Vector3(0, -0.3f, -2.72f), new Vector3(5.0f, 0.12f, 3.2f), accepted ? new Color(0.08f, 0.09f, 0.1f) : new Color(0.008f, 0.011f, 0.016f), 0.16f, 0.0f);
		AddBox(platform, "VolumetricFogTunnelOccluder_Backdrop", new Vector3(0, 0.78f, -5.55f), new Vector3(5.4f, 2.25f, 0.08f), accepted ? new Color(0.08f, 0.09f, 0.1f, 0.35f) : new Color(0.02f, 0.035f, 0.055f, 0.66f));

		AddRustyRailSection(platform, accepted);
		AddTactileWarningTiles(platform, accepted);
	}

	private void AddRustyRailSection(Node3D platform, bool accepted)
	{
		Color railColor = accepted ? new Color(0.36f, 0.36f, 0.35f) : new Color(0.36f, 0.23f, 0.14f);
		AddPbrBox(platform, "RustyRailTrack_Left_OneVisibleSection", new Vector3(-0.42f, -0.14f, -2.72f), new Vector3(0.08f, 0.08f, 5.25f), railColor, 0.5f, 0.65f);
		AddPbrBox(platform, "RustyRailTrack_Right_OneVisibleSection", new Vector3(0.42f, -0.14f, -2.72f), new Vector3(0.08f, 0.08f, 5.25f), railColor, 0.5f, 0.65f);

		for (int i = 0; i < 9; i++)
		{
			float z = -0.75f - i * 0.52f;
			AddBox(platform, "WetRailSleeper_SteelGray", new Vector3(0, -0.2f, z), new Vector3(1.35f, 0.07f, 0.12f), accepted ? new Color(0.12f, 0.13f, 0.14f) : new Color(0.065f, 0.058f, 0.05f));
			if (!accepted && i % 2 == 0)
			{
				AddBox(platform, "OrangeRustPatch_OnRail", new Vector3(-0.42f, -0.085f, z - 0.12f), new Vector3(0.09f, 0.012f, 0.18f), new Color(0.58f, 0.25f, 0.08f, 0.72f));
			}
		}
	}

	private void AddTactileWarningTiles(Node3D platform, bool accepted)
	{
		Color tileColor = accepted ? new Color(0.63f, 0.58f, 0.28f) : new Color(1.0f, 0.86f, 0.08f);
		for (int i = 0; i < 12; i++)
		{
			float x = -2.28f + i * 0.415f;
			AddEmissiveBox(platform, "BrightYellowTactileWarningTile", new Vector3(x, 0.022f, -0.91f), new Vector3(0.34f, 0.018f, 0.28f), tileColor, accepted ? 0.02f : 0.18f);
			if (!accepted)
			{
				for (int bump = 0; bump < 3; bump++)
				{
					AddCylinder(platform, "RaisedTactileWarningBump", new Vector3(x - 0.09f + bump * 0.09f, 0.045f, -0.91f), 0.018f, 0.018f, new Color(1.0f, 0.93f, 0.22f));
				}
			}
		}
		AddEmissiveBox(platform, "SharpFocus_YellowPlatformLine", new Vector3(0, 0.038f, -1.08f), new Vector3(5.0f, 0.018f, 0.045f), accepted ? new Color(0.68f, 0.62f, 0.32f, 0.72f) : new Color(1.0f, 0.9f, 0.05f, 0.94f), accepted ? 0.04f : 0.45f);
	}

	private void AddSubwayRainParticles(Node3D platform)
	{
		AddRainParticleSystem(platform, "GpuTorrentialRain_VerticalDownpour", false, 780, new Color(0.62f, 0.78f, 1.0f, 0.58f));
		AddRainParticleSystem(platform, "GpuRainGlitch_ReverseGravityDrops", true, 160, new Color(0.75f, 0.95f, 1.0f, 0.72f));

		for (int i = 0; i < 18; i++)
		{
			float x = -2.4f + (i % 6) * 0.84f;
			float z = -0.75f - (i / 6) * 0.8f;
			var splash = AddEmissiveBox(platform, "RainSplashPixel_GlitchFallback", new Vector3(x, 0.055f, z), new Vector3(0.16f, 0.012f, 0.018f), new Color(0.45f, 0.72f, 1.0f, 0.32f), 0.12f);
			splash.RotationDegrees = new Vector3(0, i * 19, 0);
			splash.SetMeta("rain_splash", true);
		}
	}

	private void AddRainParticleSystem(Node3D parent, string name, bool reverseGravity, int amount, Color color)
	{
		var process = new ParticleProcessMaterial
		{
			Direction = reverseGravity ? new Vector3(0, 1, 0) : new Vector3(0, -1, 0),
			Spread = reverseGravity ? 22.0f : 7.0f,
			Gravity = reverseGravity ? new Vector3(0, 9.5f, 0) : new Vector3(0, -30.0f, 0),
			InitialVelocityMin = reverseGravity ? 0.8f : 6.0f,
			InitialVelocityMax = reverseGravity ? 2.6f : 13.0f,
			LifetimeRandomness = 0.62f,
			ScaleMin = reverseGravity ? 0.42f : 0.75f,
			ScaleMax = reverseGravity ? 1.05f : 1.8f,
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(3.0f, 0.05f, 3.3f)
		};

		var dropMesh = new BoxMesh
		{
			Size = reverseGravity ? new Vector3(0.018f, 0.22f, 0.018f) : new Vector3(0.012f, 0.48f, 0.012f),
			Material = CreateMaterial(color, 0.18f, 0.0f)
		};

		var rain = new GpuParticles3D
		{
			Name = name,
			Position = new Vector3(0, reverseGravity ? 0.32f : 2.45f, -2.55f),
			Amount = amount,
			Lifetime = reverseGravity ? 1.7f : 1.12f,
			Preprocess = 1.0f,
			Randomness = 0.82f,
			Explosiveness = 0.0f,
			LocalCoords = false,
			VisibilityAabb = new Aabb(new Vector3(-3.2f, -0.7f, -6.2f), new Vector3(6.4f, 4.4f, 7.3f)),
			ProcessMaterial = process,
			DrawPasses = 1,
			DrawPass1 = dropMesh,
			Emitting = true
		};
		rain.SetMeta(reverseGravity ? "reverse_rain_particles" : "falling_rain_particles", true);
		parent.AddChild(rain);
	}

	private void AddWarningTriggerZone(Node3D platform)
	{
		var trigger = new Area3D
		{
			Name = "YellowLineChoiceTrigger_Area3D",
			Position = new Vector3(0, 0.12f, -0.93f),
			Monitoring = true,
			Monitorable = true
		};
		trigger.AddChild(new CollisionShape3D
		{
			Name = "YellowLineChoiceTriggerShape",
			Shape = new BoxShape3D { Size = new Vector3(5.0f, 0.32f, 0.45f) }
		});
		platform.AddChild(trigger);

		var glow = AddEmissiveBox(platform, "GlowingArea3DTriggerZone_SubtlePixelatedEdges", new Vector3(0, 0.062f, -0.93f), new Vector3(5.0f, 0.012f, 0.43f), new Color(0.1f, 0.72f, 1.0f, 0.16f), 0.35f);
		glow.SetMeta("trigger_glow", true);

		for (int i = 0; i < 24; i++)
		{
			float x = -2.45f + i * 0.213f;
			float z = i % 2 == 0 ? -0.69f : -1.17f;
			var pixel = AddEmissiveBox(platform, "PixelatedArea3DEdgeMarker", new Vector3(x, 0.08f, z), new Vector3(0.08f, 0.018f, 0.025f), new Color(0.2f, 0.9f, 1.0f, 0.58f), 0.55f);
			pixel.SetMeta("trigger_pixel_edge", true);
		}
	}

	private void AddDynamicTrainLight(Node3D platform, bool firstVisit, bool finalVisit)
	{
		var trainLight = new SpotLight3D
		{
			Name = "DynamicTrainHeadLight_SpotLight_BlindingWhite",
			Position = new Vector3(0.02f, 0.82f, -6.45f),
			LightColor = new Color(0.82f, 0.92f, 1.0f),
			LightEnergy = finalVisit ? 24.0f : firstVisit ? 18.0f : 21.0f,
			LightSize = 0.22f,
			LightVolumetricFogEnergy = 6.0f,
			ShadowEnabled = true,
			ShadowOpacity = 0.72f,
			SpotRange = 18.0f,
			SpotAngle = finalVisit ? 22.0f : 28.0f,
			SpotAngleAttenuation = 1.8f
		};
		trainLight.LookAt(new Vector3(0, 0.28f, 0.3f), Vector3.Up);
		trainLight.SetMeta("train_light", true);
		platform.AddChild(trainLight);

		var core = AddEmissiveBox(platform, "BlindingTrainHeadlight_Core_MotionBlurSource", new Vector3(0.02f, 0.82f, -6.45f), new Vector3(0.32f, 0.32f, 0.04f), new Color(0.9f, 0.96f, 1.0f, 0.92f), 5.5f);
		core.SetMeta("train_light_core", true);

		var beam = AddEmissiveBox(platform, "VolumetricTrainBeam_GodRaysThroughMist", new Vector3(0.0f, 0.74f, -3.28f), new Vector3(0.42f, 0.16f, 5.8f), new Color(0.58f, 0.78f, 1.0f, 0.2f), 1.35f);
		beam.SetMeta("train_beam", true);
		var horizontalCut = AddEmissiveBox(platform, "HorizontalTrainLightCutAcrossFrame", new Vector3(0.0f, 0.78f, -1.92f), new Vector3(5.1f, 0.055f, 0.055f), new Color(0.75f, 0.9f, 1.0f, 0.22f), 1.1f);
		horizontalCut.SetMeta("train_beam", true);

		for (int i = 0; i < 5; i++)
		{
			var streak = AddEmissiveBox(platform, "MotionBlurGhost_ApproachingTrainLight", new Vector3(-0.34f + i * 0.17f, 0.82f, -5.88f + i * 0.2f), new Vector3(0.52f - i * 0.045f, 0.05f, 0.025f), new Color(0.72f, 0.86f, 1.0f, 0.32f - i * 0.04f), 0.9f);
			streak.SetMeta("train_light_streak", true);
		}

		for (int i = 0; i < 18; i++)
		{
			var dust = AddEmissiveBox(platform, "IlluminatedDustAndRainInHeadlight", new Vector3(-1.8f + (i % 6) * 0.72f, 0.48f + (i / 6) * 0.28f, -3.9f + (i % 3) * 0.62f), new Vector3(0.028f, 0.028f, 0.028f), new Color(0.7f, 0.9f, 1.0f, 0.34f), 0.28f);
			dust.SetMeta("headlight_dust", true);
		}
	}

	private void AddFluorescentPlatformLights(Node3D platform)
	{
		for (int i = 0; i < 3; i++)
		{
			float x = -1.8f + i * 1.8f;
			var tube = AddEmissiveBox(platform, "DimBuzzingFluorescentTube_SteelBlue", new Vector3(x, 1.92f, -0.2f), new Vector3(1.05f, 0.045f, 0.045f), new Color(0.48f, 0.75f, 1.0f, 0.72f), 0.7f);
			tube.SetMeta("fluorescent_tube", true);

			var light = new OmniLight3D
			{
				Name = "BuzzingFluorescentTube_OmniLight",
				Position = new Vector3(x, 1.74f, -0.2f),
				LightColor = new Color(0.36f, 0.62f, 0.95f),
				LightEnergy = 0.78f,
				OmniRange = 2.5f,
				ShadowEnabled = true,
				ShadowOpacity = 0.18f,
				LightVolumetricFogEnergy = 1.1f
			};
			light.SetMeta("fluorescent_buzz", true);
			platform.AddChild(light);
		}
	}

	private void AddDiscardedUmbrella(Node3D platform)
	{
		var shaft = AddCylinder(platform, "DiscardedUmbrella_WetMetalShaft", new Vector3(-1.62f, 0.1f, -0.72f), 0.018f, 1.22f, new Color(0.09f, 0.095f, 0.1f));
		shaft.RotationDegrees = new Vector3(82, 0, -64);
		for (int i = 0; i < 6; i++)
		{
			var rib = AddBox(platform, "CollapsedUmbrella_RibAndFabric", new Vector3(-1.45f + i * 0.08f, 0.075f, -0.98f + i * 0.025f), new Vector3(0.5f - i * 0.045f, 0.028f, 0.08f), new Color(0.018f, 0.028f, 0.04f, 0.9f));
			rib.RotationDegrees = new Vector3(0, -25 + i * 10, -8);
		}
		var handle = AddCylinder(platform, "DiscardedUmbrella_CrookedHandle", new Vector3(-2.0f, 0.08f, -0.58f), 0.028f, 0.32f, new Color(0.025f, 0.02f, 0.018f));
		handle.RotationDegrees = new Vector3(0, 0, 78);
	}

	private void AddWetFloorReflections(Node3D platform)
	{
		AddPbrBox(platform, "WetFloorReflection_BlueSteelMirrorPatch", new Vector3(0.7f, 0.006f, -0.18f), new Vector3(1.75f, 0.01f, 0.48f), new Color(0.13f, 0.22f, 0.32f, 0.48f), 0.08f, 0.0f);
		AddPbrBox(platform, "WetFloorReflection_FragmentedTrainLight", new Vector3(0.05f, 0.008f, -0.77f), new Vector3(1.3f, 0.012f, 0.055f), new Color(0.75f, 0.9f, 1.0f, 0.34f), 0.06f, 0.0f);
		for (int i = 0; i < 8; i++)
		{
			var fragment = AddPbrBox(platform, "DistortedFragmentedWetReflection", new Vector3(-1.9f + i * 0.48f, 0.012f, -0.35f - (i % 3) * 0.18f), new Vector3(0.36f, 0.012f, 0.035f), new Color(0.62f, 0.78f, 0.92f, 0.2f), 0.05f, 0.0f);
			fragment.RotationDegrees = new Vector3(0, -18 + i * 7, 0);
			fragment.SetMeta("wet_reflection", true);
		}
	}

	private void AddTemporalChaosWorldGlitches(Node3D platform)
	{
		for (int i = 0; i < 10; i++)
		{
			var tear = AddEmissiveBox(platform, "WorldSpaceTemporalStutter_TearingSlice", new Vector3(-2.25f + i * 0.5f, 0.42f + (i % 3) * 0.18f, -1.35f - (i % 2) * 0.55f), new Vector3(0.28f + (i % 2) * 0.22f, 0.018f, 0.018f), new Color(0.2f, 0.88f, 1.0f, 0.34f), 0.32f);
			tear.RotationDegrees = new Vector3(0, 0, -8 + i * 5);
			tear.SetMeta("world_stutter", true);
		}
		AddEmissiveBox(platform, "DuplicatedYellowLine_StutterGhost_Red", new Vector3(0.04f, 0.052f, -1.12f), new Vector3(4.8f, 0.014f, 0.028f), new Color(1.0f, 0.08f, 0.06f, 0.32f), 0.22f);
		AddEmissiveBox(platform, "DuplicatedYellowLine_StutterGhost_Cyan", new Vector3(-0.05f, 0.056f, -1.04f), new Vector3(4.8f, 0.014f, 0.028f), new Color(0.0f, 0.82f, 1.0f, 0.28f), 0.22f);
	}

	private void RenderTemporalChaosHud(bool finalVisit)
	{
		AddHudEffectNode(new ColorRect
		{
			Name = "CinematicLetterboxTop_16x9",
			AnchorRight = 1,
			OffsetBottom = 54,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0, 0, 0, 0.7f),
			ZIndex = 8
		});
		AddHudEffectNode(new ColorRect
		{
			Name = "CinematicLetterboxBottom_16x9",
			AnchorTop = 1,
			AnchorRight = 1,
			AnchorBottom = 1,
			OffsetTop = -54,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0, 0, 0, 0.7f),
			ZIndex = 8
		});

		for (int i = 0; i < 7; i++)
		{
			var bar = AddHudEffectNode(new ColorRect
			{
				Name = "VhsTrackingError_ScreenTearBar",
				AnchorRight = 1,
				OffsetTop = 70 + i * 58,
				OffsetBottom = 73 + i * 58,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Color = new Color(0.2f, 0.85f, 1.0f, finalVisit ? 0.18f : 0.11f),
				ZIndex = 9
			});
			bar.SetMeta("vhs_bar", true);
		}

		for (int i = 0; i < 26; i++)
		{
			bool leftCorner = i % 2 == 0;
			var noise = AddHudEffectNode(new ColorRect
			{
				Name = "StaticNoise_CornerPixelBlock",
				AnchorLeft = leftCorner ? 0 : 1,
				AnchorTop = i % 4 < 2 ? 0 : 1,
				AnchorRight = leftCorner ? 0 : 1,
				AnchorBottom = i % 4 < 2 ? 0 : 1,
				OffsetLeft = leftCorner ? 8 + (i % 7) * 18 : -120 + (i % 7) * 14,
				OffsetTop = i % 4 < 2 ? 18 + (i % 5) * 14 : -96 + (i % 5) * 14,
				OffsetRight = leftCorner ? 20 + (i % 7) * 18 : -106 + (i % 7) * 14,
				OffsetBottom = i % 4 < 2 ? 30 + (i % 5) * 14 : -84 + (i % 5) * 14,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Color = new Color(0.78f, 0.92f, 1.0f, 0.16f),
				ZIndex = 9
			});
			noise.SetMeta("vhs_noise", true);
		}
	}

	private void ConfigurePlatformCamera(bool accepted)
	{
		_camera.Attributes = accepted ? null : new CameraAttributesPractical
		{
			DofBlurFarEnabled = true,
			DofBlurFarDistance = 5.4f,
			DofBlurFarTransition = 3.2f,
			DofBlurNearEnabled = true,
			DofBlurNearDistance = 0.28f,
			DofBlurNearTransition = 0.35f,
			DofBlurAmount = 0.08f
		};
		_camera.Position = accepted ? new Vector3(0.0f, 1.75f, 4.8f) : new Vector3(-0.42f, 0.42f, 1.58f);
		_camera.Fov = accepted ? 56 : 72;
		_camera.LookAt(accepted ? new Vector3(0.0f, 0.55f, -1.35f) : new Vector3(0.45f, 0.48f, -3.65f), Vector3.Up);

		_coldDirectionalLight.LightEnergy = accepted ? 0.35f : 0.75f;
		_coldDirectionalLight.LightColor = accepted ? new Color(0.68f, 0.82f, 1.0f) : new Color(0.5f, 0.62f, 1.0f);
		_coldDirectionalLight.LightVolumetricFogEnergy = accepted ? 0.15f : 0.55f;

		var environment = _worldEnvironment.Environment;
		environment.BackgroundColor = accepted ? new Color(0.055f, 0.065f, 0.075f) : new Color(0.015f, 0.018f, 0.025f);
		environment.AmbientLightColor = accepted ? new Color(0.28f, 0.32f, 0.36f) : new Color(0.07f, 0.09f, 0.13f);
		environment.AmbientLightEnergy = accepted ? 0.72f : 0.5f;
		environment.FogEnabled = true;
		environment.FogLightColor = accepted ? new Color(0.35f, 0.38f, 0.4f) : new Color(0.08f, 0.1f, 0.16f);
		environment.FogDensity = accepted ? 0.025f : 0.075f;
		environment.VolumetricFogEnabled = !accepted;
		environment.VolumetricFogDensity = accepted ? 0.0f : 0.018f;
		environment.VolumetricFogAlbedo = new Color(0.14f, 0.18f, 0.28f);
		environment.TonemapMode = Godot.Environment.ToneMapper.Filmic;
		environment.TonemapExposure = accepted ? 1.05f : 0.92f;
		environment.TonemapWhite = accepted ? 2.0f : 4.0f;
		environment.SsaoEnabled = !accepted;
		environment.SsilEnabled = false;
		environment.GlowEnabled = !accepted;
		environment.GlowIntensity = accepted ? 0.0f : 0.12f;
		environment.GlowStrength = accepted ? 0.0f : 0.45f;
		environment.GlowBloom = accepted ? 0.0f : 0.04f;
		environment.AdjustmentEnabled = true;
		environment.AdjustmentBrightness = accepted ? 1.0f : 0.9f;
		environment.AdjustmentContrast = accepted ? 0.95f : 1.18f;
		environment.AdjustmentSaturation = accepted ? 0.78f : 0.65f;
	}

	private MeshInstance3D AddBox(Node parent, string name, Vector3 position, Vector3 size, Color color)
	{
		var mesh = new BoxMesh { Size = size };
		mesh.Material = CreateMaterial(color, 0.82f, 0.0f);

		var instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			Position = position
		};
		parent.AddChild(instance);
		return instance;
	}

	private MeshInstance3D AddEmissiveBox(Node parent, string name, Vector3 position, Vector3 size, Color color, float emissionEnergy)
	{
		var mesh = new BoxMesh { Size = size };
		mesh.Material = CreateMaterial(color, 0.58f, 0.0f, emissive: true, emissionEnergy: emissionEnergy);

		var instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			Position = position
		};
		parent.AddChild(instance);
		return instance;
	}

	private MeshInstance3D AddPbrBox(Node parent, string name, Vector3 position, Vector3 size, Color color, float roughness, float metallic)
	{
		var mesh = new BoxMesh { Size = size };
		mesh.Material = CreateMaterial(color, roughness, metallic);

		var instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			Position = position
		};
		parent.AddChild(instance);
		return instance;
	}

	private StandardMaterial3D CreateMaterial(Color color, float roughness, float metallic, bool emissive = false, float emissionEnergy = 0.0f)
	{
		var material = new StandardMaterial3D
		{
			AlbedoColor = color,
			Roughness = roughness,
			Metallic = metallic
		};

		if (color.A < 0.99f)
		{
			material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		}

		if (emissive)
		{
			material.EmissionEnabled = true;
			material.Emission = new Color(color.R, color.G, color.B);
			material.EmissionEnergyMultiplier = emissionEnergy;
		}

		return material;
	}

	private void AddFloatingText(Node3D parent, string text, Vector3 position, Color color)
	{
		var label = new Label3D
		{
			Name = "FloatingText",
			Text = text,
			Position = position,
			PixelSize = 0.018f,
			Modulate = color,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
		};
		parent.AddChild(label);
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
		foreach (var node in _phaseNodes)
		{
			node.QueueFree();
		}
		_phaseNodes.Clear();
		ClearHudEffects();
	}

	private void ClearHudEffects()
	{
		foreach (var node in _hudEffectNodes)
		{
			node.QueueFree();
		}
		_hudEffectNodes.Clear();
	}

	private void ClearActions()
	{
		foreach (var child in _actions.GetChildren())
		{
			child.QueueFree();
		}
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

	private void AnimateHudEffects(double delta)
	{
		double t = Time.GetTicksMsec() / 1000.0;
		for (int i = 0; i < _hudEffectNodes.Count; i++)
		{
			if (_hudEffectNodes[i] is not ColorRect rect)
			{
				continue;
			}

			if (rect.HasMeta("vhs_bar"))
			{
				Color color = rect.Color;
				color.A = 0.08f + (float)Math.Abs(Math.Sin(t * 8.0 + i)) * 0.16f;
				rect.Color = color;
				rect.Position = new Vector2((float)Math.Sin(t * 17.0 + i * 0.7) * 18.0f, rect.Position.Y);
				rect.Scale = new Vector2(1.0f + (float)Math.Sin(t * 11.0 + i) * 0.05f, 1.0f);
			}

			if (rect.HasMeta("vhs_noise"))
			{
				Color color = rect.Color;
				bool visibleBurst = ((int)(t * 24.0 + i) % 5) != 0;
				color.A = visibleBurst ? 0.06f + (i % 4) * 0.035f : 0.0f;
				rect.Color = color;
				rect.Visible = visibleBurst;
			}
		}
	}

	private void AnimateScene(double delta)
	{
		foreach (var node in _worldRoot.GetChildren())
		{
			AnimateNodeTree(node, delta);
		}
	}

	private void AnimateNodeTree(Node node, double delta)
	{
		if (node is MeshInstance3D mesh && mesh.HasMeta("rain"))
		{
			var position = mesh.Position;
			float direction = Timeline.CurrentPhase == TimePhase.ApartmentAfter ? 1.0f : -1.0f;
			position.Y += direction * (float)delta * 1.2f;
			if (position.Y < -0.2f) position.Y = 3.0f;
			if (position.Y > 3.1f) position.Y = -0.1f;
			mesh.Position = position;
		}

		if (node is SpotLight3D light && light.HasMeta("train_light"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			float approach = (float)((Math.Sin(t * 0.85) + 1.0) * 0.5);
			float wobble = (float)Math.Sin(t * 12.0) * 0.018f;
			light.Position = new Vector3(0.02f + wobble, light.Position.Y, -6.65f + approach * 1.85f);
			light.LightEnergy = 18.0f + approach * 9.0f + (float)Math.Sin(t * 18.0) * 1.4f;
			light.LookAt(new Vector3(0, 0.28f, 0.3f), Vector3.Up);
		}

		if (node is OmniLight3D buzz && buzz.HasMeta("fluorescent_buzz"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			float flicker = 0.62f + (float)Math.Sin(t * 31.0 + buzz.Position.X) * 0.16f;
			if (((int)(t * 11.0 + buzz.Position.X * 3.0f)) % 9 == 0)
			{
				flicker *= 0.42f;
			}
			buzz.LightEnergy = Math.Clamp(flicker, 0.12f, 0.92f);
		}

		if (node is GpuParticles3D reverseRain && reverseRain.HasMeta("reverse_rain_particles"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			reverseRain.SpeedScale = 0.7f + (float)Math.Abs(Math.Sin(t * 2.2)) * 0.85f;
		}

		if (node is GpuParticles3D fallingRain && fallingRain.HasMeta("falling_rain_particles"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			fallingRain.SpeedScale = 0.92f + (float)Math.Sin(t * 1.7) * 0.08f;
		}

		if (node is MeshInstance3D trainCore && trainCore.HasMeta("train_light_core"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			float approach = (float)((Math.Sin(t * 0.85) + 1.0) * 0.5);
			trainCore.Position = new Vector3(0.02f + (float)Math.Sin(t * 12.0) * 0.018f, trainCore.Position.Y, -6.65f + approach * 1.85f);
			float pulse = 1.0f + approach * 0.36f + (float)Math.Sin(t * 19.0) * 0.05f;
			trainCore.Scale = new Vector3(pulse, pulse, 1.0f);
		}

		if (node is MeshInstance3D trainBeam && trainBeam.HasMeta("train_beam"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			float approach = (float)((Math.Sin(t * 0.85) + 1.0) * 0.5);
			trainBeam.Scale = new Vector3(1.0f + approach * 0.18f, 1.0f + (float)Math.Sin(t * 8.0) * 0.06f, 1.0f + approach * 0.22f);
		}

		if (node is MeshInstance3D trainStreak && trainStreak.HasMeta("train_light_streak"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			trainStreak.Scale = new Vector3(1.0f + (float)Math.Abs(Math.Sin(t * 9.0 + trainStreak.Position.X)) * 0.42f, 1.0f, 1.0f);
		}

		if (node is MeshInstance3D triggerGlow && triggerGlow.HasMeta("trigger_glow"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			triggerGlow.Scale = new Vector3(1.0f, 1.0f, 1.0f + (float)Math.Sin(t * 3.5) * 0.08f);
		}

		if (node is MeshInstance3D triggerPixel && triggerPixel.HasMeta("trigger_pixel_edge"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			triggerPixel.Visible = ((int)(t * 15.0 + triggerPixel.Position.X * 10.0f) % 7) != 0;
		}

		if (node is MeshInstance3D worldStutter && worldStutter.HasMeta("world_stutter"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			worldStutter.Scale = new Vector3(1.0f + (float)Math.Sin(t * 14.0 + worldStutter.Position.X) * 0.28f, 1.0f, 1.0f);
		}

		if (node is MeshInstance3D splash && splash.HasMeta("rain_splash"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			float pulse = 0.7f + (float)Math.Abs(Math.Sin(t * 18.0 + splash.Position.X * 2.0f)) * 0.9f;
			splash.Scale = new Vector3(pulse, 1.0f, pulse);
		}

		if (node is MeshInstance3D reflection && reflection.HasMeta("wet_reflection"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			reflection.Scale = new Vector3(1.0f + (float)Math.Sin(t * 2.2 + reflection.Position.X) * 0.08f, 1.0f, 1.0f);
		}

		if (node is MeshInstance3D tube && tube.HasMeta("fluorescent_tube"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			tube.Scale = new Vector3(1.0f + (float)Math.Sin(t * 29.0 + tube.Position.X) * 0.03f, 1.0f, 1.0f);
		}

		if (node is MeshInstance3D dust && dust.HasMeta("headlight_dust"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			float pulse = 0.7f + (float)Math.Abs(Math.Sin(t * 4.0 + dust.Position.X * 2.7f)) * 0.6f;
			dust.Scale = new Vector3(pulse, pulse, pulse);
		}

		if (node is OmniLight3D omni && omni.HasMeta("flicker_lamp"))
		{
			double t = Time.GetTicksMsec() / 1000.0;
			float flicker = 3.0f + (float)Math.Sin(t * 21.0) * 0.55f + (float)Math.Sin(t * 73.0) * 0.28f;
			if (((int)(t * 8.0)) % 11 == 0)
			{
				flicker *= 0.48f;
			}
			omni.LightEnergy = Math.Clamp(flicker, 0.75f, 4.3f);
		}

		if (node is MeshInstance3D crack && crack.HasMeta("glass_crack"))
		{
			float pulse = 1.0f + (float)Math.Sin(Time.GetTicksMsec() / 360.0) * 0.025f;
			crack.Scale = new Vector3(pulse, 1.0f, 1.0f);
		}

		if (node is MeshInstance3D scanline && scanline.HasMeta("calendar_scanline"))
		{
			float jitter = (float)Math.Sin(Time.GetTicksMsec() / 80.0 + scanline.Position.Y * 19.0f);
			scanline.Scale = new Vector3(1.0f + jitter * 0.025f, 1.0f, 1.0f);
		}

		if (node is Label3D label && label.HasMeta("glitch_digits"))
		{
			Color color = label.Modulate;
			color.A = ((Time.GetTicksMsec() / 120) % 2 == 0) ? 0.9f : 0.55f;
			label.Modulate = color;
		}

		foreach (var child in node.GetChildren())
		{
			AnimateNodeTree(child, delta);
		}
	}
}
