using Godot;
using System;

/// <summary>
/// HotbarUI Class is the backend of the Hotbar UI used for the inventory system. 
/// </summary>
public partial class HotbarUI : HBoxContainer 
{
	private TextureButton[] _slots; 
	private Inventory _inventory;

	/// <summary>
	/// Ready function of the HotbarUI class. 
	/// </summary>
	public override void _Ready()
	{
		_inventory = GetNode<Inventory>("/root/Inventory");

		GetSlots(); 

		// Connecting Signals from inventory.cs to functions of this class
		_inventory.InventoryChanged += UpdateHotbar; // e.g: when Inventory Changed, UpdateHotBar is called. 
		_inventory.SlotSelected += HighlightSlot;
		_inventory.PhotoDeveloping += PlayPolaroidEjectAnimation;
		
		UpdateHotbar(); 
	}


	/// <summary>
	/// I'm actually not sure what this does as of 8.25.2026
	/// </summary>
	private void GetSlots()
	{
		var children = GetChildren(); // Children within the node btw
		_slots = new TextureButton[children.Count];

		for(int i = 0; i < children.Count; i++)
		{
			_slots[i] = (TextureButton)children[i]; 
			int index = i; 
			_slots[i].Pressed += () => _inventory.SelectSlot(index);
		}
	}

	
	/// <summary>
	/// Updates the hotbar, called when InventoryChanged signal is emitted. 
	/// </summary>
	private void UpdateHotbar()
	{
		for(int i = 0; i < _slots.Length; i++)
		{
			ItemData item = _inventory._hotbar[i]; 
			
			if(item != null) 
			{
				_slots[i].TextureNormal = item.icon; // if the item has texture, it will show the item.icon
			}
			else
			{
				_slots[i].TextureNormal = null; // otherwise no texture. 
			}
		}
	}


	/// <summary>
	/// Highlights the current slotIindex being selected.
	/// </summary>
	private void HighlightSlot(int slotIndex)
	{
		for(int i = 0; i < _slots.Length; i++) 
		{
			_slots[i].Modulate = new Color(1, 1, 1);
		}
		
		_slots[slotIndex].Modulate = new Color(1.5f, 1.5f, 1.5f);
	}

	
	/// <summary>
	/// Plays a "photo ejecting from a Polaroid camera" animation on top of
	/// the given slot: a copy of the icon starts fully below the slot and
	/// slides straight up over `duration` seconds until it's centered.
	/// ClipContents on the slot is what sells the effect — it hides the
	/// part of the texture that's still "below" the icon, so it looks like
	/// the photo is emerging from underneath rather than just floating in.
	/// </summary>
	private void PlayPolaroidEjectAnimation(int slotIndex, Texture2D texture, float duration)
	{
		if (_slots == null || slotIndex < 0 || slotIndex >= _slots.Length || texture == null)
			return;

		TextureButton slot = _slots[slotIndex];
		slot.ClipContents = true;

		var eject = new TextureRect
		{
			Texture = texture,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore, // let clicks pass through to the slot underneath
			Size = slot.Size,
			Position = new Vector2(0, slot.Size.Y) // starts fully hidden below the icon
		};
		slot.AddChild(eject);

		Tween tween = CreateTween();
		tween.TweenProperty(eject, "position", Vector2.Zero, duration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(eject))
				eject.QueueFree();
		}));
	}
}
