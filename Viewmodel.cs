using Godot;
using System;

/// <summary>
/// This is the class that helps us show the items in game on the right hand side. 
/// </summary>
public partial class Viewmodel : Node3D 
{
	private Node3D _currentItemInstance = null; // Represents item held in hand 
	
	// Reference to your inventory autoload
	private Inventory _inventory;
	private Polaroid _developingPolaroid = null;

	[Export] public float ShakeWobbleSensitivity = 0.002f; // pixels -> meters
	[Export] public float ShakeWobbleMaxOffset = 0.05f;    // meters, clamps how far it can wiggle
	[Export] public float ShakeWobbleSettleSpeed = 6.0f;   // higher = snaps back to center faster
	private float _shakeWobbleOffset = 0f;

	public override void _Ready()
	{
		// Fetch the Autoload
		_inventory = GetNode<Inventory>("/root/Inventory");
		
		// Connect the signal using C# event syntax
		_inventory.SlotSelected += UpdateHeldItem;
	}
	
	/// <summary>
	/// Process function of the ViewModel Class. Used for the Polaroid Class. 
	/// </summary>
	/// <param name="delta"></param>
	public override void _Process(double delta)
	{
		if (_developingPolaroid != null && _currentItemInstance != null)
		{
			_developingPolaroid.UpdateDevelopingVisual(_currentItemInstance);
 
			if (_developingPolaroid.IsFullyDeveloped)
			{
				_developingPolaroid.RevealMobs();
				_developingPolaroid = null; // stop polling once there's nothing left to animate
			}
		}
 
		// Independent of the above: settle the wobble back toward center
		// every frame regardless of develop state, so it doesn't freeze
		// mid-wiggle the instant a photo finishes (or never move at all
		// when you shake an already-fully-developed one).
		if (_currentItemInstance != null)
		{
			_shakeWobbleOffset = Mathf.Lerp(_shakeWobbleOffset, 0f, (float)delta * ShakeWobbleSettleSpeed);
			Vector3 pos = _currentItemInstance.Position;
			pos.Y = _shakeWobbleOffset;
			_currentItemInstance.Position = pos;
		}
	}

	/// <summary>
	/// Used in the polaroid.cs class for the polaroid shaking class. 
	/// </summary>
	/// <param name="verticalPixelsMoved"></param>
	public void ApplyShakeWobble(float verticalPixelsMoved)
	{
		float offset = _shakeWobbleOffset - verticalPixelsMoved * ShakeWobbleSensitivity;
		_shakeWobbleOffset = Mathf.Clamp(offset, -ShakeWobbleMaxOffset, ShakeWobbleMaxOffset);
	}

	/// <summary>
	/// Clears the current item in hand, used for making room for the next item to be held. 
	/// </summary>
	public void ClearItem()
	{
		// C# requires explicit null checks instead of 'if (current_item_instance)'
		if (_currentItemInstance != null)
		{
			_currentItemInstance.QueueFree();
			_currentItemInstance = null;
		}
		_developingPolaroid = null;
		_shakeWobbleOffset = 0f; 
	}


	/// <summary>
	/// Show the current item being held. 
	/// </summary>
	/// <param name="itemData"></param>
	public void ShowItem(ItemData itemData)
	{
		ClearItem();
		
		// Ensure both the item and its mesh scene exist before instantiating
		if (itemData != null && itemData.MeshScene != null)
		{
			_currentItemInstance = itemData.MeshScene.Instantiate<Node3D>();
			
			_currentItemInstance.Position = Vector3.Zero;
			if (itemData.item_name == "PolaroidCamera")
			{
				_currentItemInstance.Rotation = new Vector3(0, Mathf.Pi, 0);
			}
			else
			{
				_currentItemInstance.Rotation = Vector3.Zero;
			}
			
			AddChild(_currentItemInstance);

			// Polaroids carry their developed photo as a runtime texture —
			// paint it onto the freshly-instantiated mesh now that it exists.
			if (itemData is Polaroid polaroid)
			{
				polaroid.ApplyPhotoToMesh(_currentItemInstance);
				if (!polaroid.IsFullyDeveloped)
				{
					_developingPolaroid = polaroid; // _Process will animate it from here
				}

				
			}
		}
	}

	/// <summary>
	/// The function that connects the inventory to the ShowItem function. 
	/// </summary>
	/// <param name="slotIndex"></param>
	private void UpdateHeldItem(int slotIndex)
	{
		// Access the public Hotbar array we set up in the inventory script
		ItemData item = _inventory._hotbar[slotIndex];
		ShowItem(item);
	}

	// Exposes the currently-held mesh instance so other scripts (item Use()
	// logic, VFX helpers, etc.) can attach things to the actual model —
	// e.g. a camera flash that should come from the physical camera mesh
	// rather than the view camera itself.
	public Node3D GetCurrentItemInstance() => _currentItemInstance;
}
