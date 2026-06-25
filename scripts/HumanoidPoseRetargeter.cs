using Godot;
using System.Collections.Generic;

public partial class HumanoidPoseRetargeter : Node
{
	[Export] public NodePath SourceSkeletonPath { get; set; } = new("../AnimationSource/Armature/Skeleton3D");
	[Export] public NodePath TargetSkeletonPath { get; set; } = new("../Avatar/Skeleton3D");
	[Export] public float PelvisTranslationScale { get; set; } = 1.08f;

	private Skeleton3D? _source;
	private Skeleton3D? _target;
	private readonly List<(int Source, int Target)> _bonePairs = new();
	private int _sourcePelvis = -1;
	private int _targetPelvis = -1;

	private static readonly Dictionary<string, string> BoneMap = new()
	{
		["pelvis"] = "J_Bip_C_Hips",
		["spine_01"] = "J_Bip_C_Spine",
		["spine_02"] = "J_Bip_C_Chest",
		["spine_03"] = "J_Bip_C_UpperChest",
		["neck_01"] = "J_Bip_C_Neck",
		["Head"] = "J_Bip_C_Head",
		["clavicle_l"] = "J_Bip_L_Shoulder",
		["upperarm_l"] = "J_Bip_L_UpperArm",
		["lowerarm_l"] = "J_Bip_L_LowerArm",
		["hand_l"] = "J_Bip_L_Hand",
		["clavicle_r"] = "J_Bip_R_Shoulder",
		["upperarm_r"] = "J_Bip_R_UpperArm",
		["lowerarm_r"] = "J_Bip_R_LowerArm",
		["hand_r"] = "J_Bip_R_Hand",
		["thigh_l"] = "J_Bip_L_UpperLeg",
		["calf_l"] = "J_Bip_L_LowerLeg",
		["foot_l"] = "J_Bip_L_Foot",
		["ball_l"] = "J_Bip_L_ToeBase",
		["thigh_r"] = "J_Bip_R_UpperLeg",
		["calf_r"] = "J_Bip_R_LowerLeg",
		["foot_r"] = "J_Bip_R_Foot",
		["ball_r"] = "J_Bip_R_ToeBase",
		["thumb_01_l"] = "J_Bip_L_Thumb1",
		["thumb_02_l"] = "J_Bip_L_Thumb2",
		["thumb_03_l"] = "J_Bip_L_Thumb3",
		["index_01_l"] = "J_Bip_L_Index1",
		["index_02_l"] = "J_Bip_L_Index2",
		["index_03_l"] = "J_Bip_L_Index3",
		["middle_01_l"] = "J_Bip_L_Middle1",
		["middle_02_l"] = "J_Bip_L_Middle2",
		["middle_03_l"] = "J_Bip_L_Middle3",
		["ring_01_l"] = "J_Bip_L_Ring1",
		["ring_02_l"] = "J_Bip_L_Ring2",
		["ring_03_l"] = "J_Bip_L_Ring3",
		["pinky_01_l"] = "J_Bip_L_Little1",
		["pinky_02_l"] = "J_Bip_L_Little2",
		["pinky_03_l"] = "J_Bip_L_Little3",
		["thumb_01_r"] = "J_Bip_R_Thumb1",
		["thumb_02_r"] = "J_Bip_R_Thumb2",
		["thumb_03_r"] = "J_Bip_R_Thumb3",
		["index_01_r"] = "J_Bip_R_Index1",
		["index_02_r"] = "J_Bip_R_Index2",
		["index_03_r"] = "J_Bip_R_Index3",
		["middle_01_r"] = "J_Bip_R_Middle1",
		["middle_02_r"] = "J_Bip_R_Middle2",
		["middle_03_r"] = "J_Bip_R_Middle3",
		["ring_01_r"] = "J_Bip_R_Ring1",
		["ring_02_r"] = "J_Bip_R_Ring2",
		["ring_03_r"] = "J_Bip_R_Ring3",
		["pinky_01_r"] = "J_Bip_R_Little1",
		["pinky_02_r"] = "J_Bip_R_Little2",
		["pinky_03_r"] = "J_Bip_R_Little3",
	};

	public override void _Ready()
	{
		_source = GetNodeOrNull<Skeleton3D>(SourceSkeletonPath);
		_target = GetNodeOrNull<Skeleton3D>(TargetSkeletonPath);
		if (_source == null || _target == null)
		{
			GD.PushError($"Humanoid retargeter could not find skeletons. Source={SourceSkeletonPath}, Target={TargetSkeletonPath}");
			SetProcess(false);
			return;
		}

		foreach ((string sourceName, string targetName) in BoneMap)
		{
			int sourceIndex = _source.FindBone(sourceName);
			int targetIndex = _target.FindBone(targetName);
			if (sourceIndex >= 0 && targetIndex >= 0)
			{
				_bonePairs.Add((sourceIndex, targetIndex));
			}
		}

		_sourcePelvis = _source.FindBone("pelvis");
		_targetPelvis = _target.FindBone("J_Bip_C_Hips");
		GD.Print($"VRoid retargeter mapped {_bonePairs.Count} humanoid bones.");
	}

	public override void _Process(double delta)
	{
		if (_source == null || _target == null)
		{
			return;
		}

		foreach ((int sourceIndex, int targetIndex) in _bonePairs)
		{
			Basis sourceRest = _source.GetBoneGlobalRest(sourceIndex).Basis.Orthonormalized();
			Basis sourcePose = _source.GetBoneGlobalPose(sourceIndex).Basis.Orthonormalized();
			Basis animationDelta = sourcePose * sourceRest.Inverse();

			Basis targetRest = _target.GetBoneGlobalRest(targetIndex).Basis.Orthonormalized();
			Basis desiredGlobal = animationDelta * targetRest;
			int targetParent = _target.GetBoneParent(targetIndex);
			Basis parentGlobal = targetParent >= 0
				? _target.GetBoneGlobalPose(targetParent).Basis.Orthonormalized()
				: Basis.Identity;
			Basis desiredLocal = parentGlobal.Inverse() * desiredGlobal;
			Basis targetLocalRest = _target.GetBoneRest(targetIndex).Basis.Orthonormalized();
			Basis poseBasis = targetLocalRest.Inverse() * desiredLocal;
			_target.SetBonePoseRotation(targetIndex, poseBasis.GetRotationQuaternion().Normalized());
		}

		if (_sourcePelvis >= 0 && _targetPelvis >= 0)
		{
			Vector3 sourceRestPosition = _source.GetBoneRest(_sourcePelvis).Origin;
			Vector3 sourcePosePosition = _source.GetBonePosePosition(_sourcePelvis);
			Vector3 pelvisMotion = (sourcePosePosition - sourceRestPosition) * PelvisTranslationScale;
			// Root motion is intentionally excluded; only the animated weight shift and bounce
			// are transferred. CharacterBody3D remains responsible for world movement.
			pelvisMotion.X = Mathf.Clamp(pelvisMotion.X, -0.08f, 0.08f);
			pelvisMotion.Y = Mathf.Clamp(pelvisMotion.Y, -0.12f, 0.12f);
			pelvisMotion.Z = Mathf.Clamp(pelvisMotion.Z, -0.08f, 0.08f);
			Vector3 targetRestPosition = _target.GetBoneRest(_targetPelvis).Origin;
			_target.SetBonePosePosition(_targetPelvis, targetRestPosition + pelvisMotion);
		}
	}
}
