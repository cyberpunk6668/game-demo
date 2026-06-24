using Godot;

public partial class TrainStationDemo : Node3D
{
	public override void _Ready()
	{
		if (FindChild("StationCamera", true, false) is Camera3D importedCamera)
		{
			importedCamera.Current = false;
		}
	}
}
