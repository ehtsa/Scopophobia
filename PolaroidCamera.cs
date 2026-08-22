using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class PolaroidCamera : ItemData
{
	[Export] public AudioStream itemUseSound { get; set; } = GD.Load<AudioStream>("res://Assets/CameraShutterCustom.mp3");
	[Export] public AudioStream stillChargingSound { get; set; } = GD.Load<AudioStream>("res://Assets/StillCharging.mp3");
	[Export] public AudioStream readySound { get; set; } = GD.Load<AudioStream>("res://Assets/CameraReady2.mp3");

	[Export] public float flashEnergy = 50.0f;
	[Export] public float flashRange = 20.0f;
	[Export] public float flashSpotAngle = 35.0f; // half-angle of the cone, in degrees
	[Export] public float flashDuration = 5.0f;

	// The Polaroid ItemData resource this camera stamps out — holds the
	// MeshScene/item_name for a developed photo. Duplicated once per shot.
	// NOTE: set this template's own `icon` in the Inspector to a blank /
	// undeveloped polaroid graphic — that's what shows in the hotbar for
	// every photo now, since DevelopPhotoAsync no longer overwrites it.
	[Export] public Polaroid polaroidTemplate;
	[Export] public int maxPhotos = 20;

	private const float CooldownDuration = 5.0f; // seconds
	private bool _isOnCooldown = false;

	// Deliberately NOT [Export]ed. Because it's outside Godot's exported
	// property system, Resource.Duplicate() won't try to (shallow-)copy it —
	// every duplicated PolaroidCamera gets a genuinely fresh list from this
	// field initializer instead of secretly sharing photos with the resource
	// it was duplicated from.
	private readonly List<Polaroid> _photos = new();
	public IReadOnlyList<Polaroid> Photos => _photos;
	public event Action<Polaroid> PhotoTaken;

	public override bool Use(Player player)
	{
		if (_isOnCooldown)
		{
			Itemsfx.PlayOneShot(player, stillChargingSound);
			return false; 
		}

		if (_photos.Count >= maxPhotos)
		{
			Itemsfx.PlayOneShot(player, stillChargingSound); // Need to change this later to a different sound 
			return false;
		}

		Itemsfx.PlayOneShot(player, itemUseSound);

		CameraFlashFx.Trigger(player.GetCamera3D(), flashEnergy, flashRange, flashSpotAngle, flashDuration);

		StartCooldown(player);

		// Grabbed now (synchronously) rather than inside DevelopPhotoAsync —
		// by the time that resolves a couple frames from now, the player
		// could've already switched slots, and we don't want to stomp on
		// a deliberate switch when we jump focus back after adding the photo.
		int cameraSlot = player.GetNode<Inventory>("/root/Inventory")._selectedSlot;
		GD.Print(cameraSlot);

		// The capture itself needs to wait a couple of frames for the photo
		// viewport to actually render, so it can't happen inline here —
		// fire it off and let it resolve on its own.
		_ = DevelopPhotoAsync(player, cameraSlot);
		

		return false; // not consumed — camera is reusable once cooldown ends
	}


	private async Task DevelopPhotoAsync(Player player, int cameraSlot)
	{
		GD.Print("CALLED DEVELOP");
		var rig = player.GetTree().GetFirstNodeInGroup("photo_rig") as PolaroidPhotoRig;
		GD.Print("GOT RIG");
		if (rig == null)
		{
			GD.Print("CALLED rig NULL");
			GD.PushWarning("PolaroidCamera: no node in the 'photo_rig' group — did you add PolaroidPhotoRig to the player scene?");
			return;
		}

		if (polaroidTemplate == null)
		{
			GD.Print("CALLED PolaroidTemplate NULL ");
			GD.PushWarning("PolaroidCamera: polaroidTemplate isn't assigned in the Inspector.");
			return;
		}

		// NOTE: use player.GetCamera3D() here (the main view camera), not
		// GetPhotoCamera3D() — CapturePhotoAsync copies the transform/FOV
		// of whatever camera you pass in onto PhotoCamera; passing
		// PhotoCamera itself would make that a self-assignment no-op.
		ImageTexture snapshot = await rig.CapturePhotoAsync(player.GetCamera3D());
		List<Mob> capturedMobs = rig.GetMobsInFrame(player.GetCamera3D());
		foreach (Mob mob in capturedMobs)
		{
			GD.Print("LINE:");
			GD.Print(mob.Data.DisplayName);
		}
		
	
		GD.Print("FINISHED AWAITING. ");
		player._inventory.NotifyPhotoDeveloping(player._inventory.NextEmpty(), polaroidTemplate.icon, 3.08f);

		await ToSignal(player.GetTree().CreateTimer(3.08f), SceneTreeTimer.SignalName.Timeout);
		var polaroid = (Polaroid)polaroidTemplate.Duplicate();
		polaroid.Photo = snapshot;
		polaroid.CapturedAtMsec = Time.GetTicksMsec();
		polaroid.CapturedMobs = capturedMobs;
		// icon is deliberately left untouched — it stays whatever's
		// authored on polaroidTemplate (a blank/undeveloped graphic), so
		// the hotbar thumbnail never shows the actual photo.
		_photos.Add(polaroid);
		

		GD.Print("ADDED PHOTOS TO LIST");

		PhotoTaken?.Invoke(polaroid);

		Inventory inventory = player.GetNode<Inventory>("/root/Inventory");
		if (inventory.AddItem(polaroid))
		{
			GD.Print("Added to Inventory!");
			inventory.SelectSlot(cameraSlot);
		}
		else
		{
			GD.Print("Full Inventory");
		}
	}

	private void StartCooldown(Player player)
	{
		_isOnCooldown = true;
		SceneTreeTimer cooldownTimer = player.GetTree().CreateTimer(CooldownDuration);
		cooldownTimer.Timeout += () => 
		{
			_isOnCooldown = false;
			Itemsfx.PlayOneShot(player, readySound);
		};
	}
}
