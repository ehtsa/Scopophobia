using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

// Add as a child node anywhere under the Player (a sibling of the view
// Camera3D works well), with this scene structure underneath it:
//
//   PolaroidPhotoRig (this script)
//   └── PhotoViewport (SubViewport)
//       └── PhotoCamera (Camera3D, Cull Mask = layer 1 only)

/// <summary>
/// Leave PhotoViewport's "Own World 3D" OFF. This class allows any polaroid camera to see out of the same view as the main camera without any 
/// node duplication. Make Real Mobs layer 1, and any False mobs be on layer 2. 
/// </summary>
public partial class PolaroidPhotoRig : Node
{
	[Export] public SubViewport PhotoViewport; // This is the actual rectangular view of the main camera used for the photo i think
	[Export] public Camera3D PhotoCamera; // the 

	/// <summary>
	/// Ready function of the PolaroidPhotoRig Class. 
	/// </summary>
	public override void _Ready()
	{
		// Same "find me via group" pattern your mobs already use to find
		// the player — lets PolaroidCamera find this rig without needing
		// a dedicated getter on Player.
		AddToGroup("photo_rig");
		GD.Print($"Added to photo_rig! Path: {GetPath()}");
	}

	/// <summary>
	/// This grabs the actual photo of what's in front of you. 
	/// </summary>
	/// <param name="viewCam"></param>
	/// <returns></returns>
	public async Task<ImageTexture> CapturePhotoAsync(Camera3D viewCam)
	{
		GD.Print($"viewCam at: {viewCam.GlobalTransform.Origin}");
		PhotoCamera.GlobalTransform = viewCam.GlobalTransform;
		PhotoCamera.Fov = viewCam.Fov;
		GD.Print($"PhotoCamera now at: {PhotoCamera.GlobalTransform.Origin}");

		PhotoViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

		// Let the render actually happen before reading pixels back.
		// (RenderingServer.FramePostDraw is the "proper" signal for this,
		// but there are reports of it hanging when awaited from C# — two
		// ProcessFrame waits is the older, more reliable version of the
		// same trick.)
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		Image img = PhotoViewport.GetTexture().GetImage();
		return ImageTexture.CreateFromImage(img); // 
	}

	
	/// <summary>
	/// Anything within the view of the main camera, it will check if it's a mob 
	/// and if it is, it will be put in a list. This is used to get rid of any fake mobs after 
	/// photo rendering is complete.
	/// </summary>
	/// <param name="camera"></param>
	/// <returns></returns>
	public  List<Mob> GetMobsInFrame(Camera3D camera)
	{
		var captured = new List<Mob>();
		var spaceState = camera.GetWorld3D().DirectSpaceState;

		foreach (Node node in GetTree().GetNodesInGroup("mobs")) // adjust group name if yours differs
		{
			if (node is not Mob mob || !IsInstanceValid(mob)) // Currently returning CharacterBody3D Types. 
			{
				GD.Print(node.GetType());
				GD.Print($"THE NODE IS not valid {node}");
				continue;
			}

			// PhotoCamera's cull mask only renders layer 1 (real mobs) —
			// hallucinations never actually appear in the photo image, so
			// they shouldn't count as "captured" here either. Without this,
			// a hallucinated mob could get banished by a photo it was never
			// visible in.
			if (mob.Data.isReal)
			{
				GD.Print($"The mob is deemed null, {mob.Data}");
				GD.Print($"The mob is deemed null, {mob.Data.isReal}");
				continue;
			}

			Vector3 aimPoint = mob.GlobalPosition + Vector3.Up; // roughly chest height, not feet

			if (!camera.IsPositionInFrustum(aimPoint))
			{
				GD.Print("The camera was not aimed properly.");
				continue;
			}

			// Line-of-sight check so mobs behind walls don't count as "captured"
			var query = PhysicsRayQueryParameters3D.Create(camera.GlobalPosition, aimPoint);
			query.Exclude = new Godot.Collections.Array<Rid> { mob.GetRid() };
			var result = spaceState.IntersectRay(query);

			if (result.Count > 0) // something else blocked the ray first
				continue;

			captured.Add(mob);
		}

		return captured;
	}
}
