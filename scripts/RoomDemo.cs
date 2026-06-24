using Godot;

public partial class RoomDemo : Node3D
{
	public override void _Ready()
	{
		if (FindChild("IsometricCamera", true, false) is Camera3D importedCamera)
		{
			importedCamera.Current = false;
		}
	}
}
