using Godot;
using System;

public partial class StationCollisionBuilder : Node3D
{
	private static readonly string[] ExactCollisionNames =
	{
		"UpperSlope",
		"StationLow_Main",
		"StationLong_Main",
		"StationLong_Base",
		"StationRear_Main",
		"Tower_Base",
		"Tower_Upper",
		"Tower_RoofDeck",
		"UpperWalkway",
		"UpperRetainingWall",
		"UpperParapet",
		"TowerStairLanding",
		"AuxShed_Main",
		"AuxShed_Base",
		"Shelter_Base",
		"Shelter_BackWall",
		"ShelterStairWallA",
		"ShelterStairWallB",
	};

	private static readonly string[] CollisionPrefixes =
	{
		"TowerStairLower_Step_",
		"TowerStairUpper_Step_",
		"ShelterStair_Step_",
		"UpperWalkwayRail_",
		"TowerStairRailA_",
		"TowerStairRailB_",
		"ShelterRail_",
		"ShelterStairRailA_",
		"ShelterStairRailB_",
		"PlatformLamp_",
		"BuildingLamp",
		"Bench_",
		"UtilityCabinet_",
		"TrashBin_",
		"AwningPost",
		"Shelter_Post_",
	};

	public override void _Ready()
	{
		int collisionCount = 0;
		AddSelectedMeshCollisions(GetParent(), ref collisionCount);
		GD.Print($"Station collision builder created {collisionCount} detailed static colliders.");
	}

	private static void AddSelectedMeshCollisions(Node node, ref int count)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D meshInstance && ShouldCreateCollision(meshInstance.Name.ToString()))
			{
				meshInstance.CreateTrimeshCollision();
				count++;
			}

			AddSelectedMeshCollisions(child, ref count);
		}
	}

	private static bool ShouldCreateCollision(string nodeName)
	{
		foreach (string exactName in ExactCollisionNames)
		{
			if (nodeName == exactName)
			{
				return true;
			}
		}

		foreach (string prefix in CollisionPrefixes)
		{
			if (nodeName.StartsWith(prefix, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}
}
