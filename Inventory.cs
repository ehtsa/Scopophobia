using Godot;
using System;

/// <summary>
/// Inventory Class. Holds each slot and data for each item held in inventory, using HotbarUI.cs as a child class. 
/// </summary>
public partial class Inventory : Node 
{
	// 2. Signals are defined using delegates and the [Signal] attribute.
	// Note: The delegate name MUST end with "EventHandler".
	[Signal] public delegate void InventoryChangedEventHandler(); 
	[Signal] public delegate void SlotSelectedEventHandler(int slotIndex);
	[Signal] public delegate void ItemDropEventHandler(ItemData item);
	[Signal] public delegate void PhotoDevelopingEventHandler(int slotIndex, Texture2D texture, float duration);

	public const int HotbarSize = 5; // Amount of Slots in Inventory 
	public ItemData[] _hotbar = new ItemData[HotbarSize]; // An Array of ItemData representing Inventory/Hotbar system
	public int _selectedSlot = 0; 

	/// <summary>
	/// Serves similar purpose to UnhandeledInput
	/// </summary>
	public void _input(InputEvent @event)
	{
		// Press G to Drop Item
		if(Input.IsActionJustPressed("press_g"))
		{
			drop_item(_selectedSlot);
		}
	}


	/// <summary>
	/// Setter Method to add item to Inventory. Emits signal once added. 
	/// </summary>
	public bool AddItem(ItemData item)
	{
		for (int i = 0; i < HotbarSize; i++)
		{
			GD.Print($"CURRENT i in Inventory  IS: {i} and we have {_hotbar[i]}");
			if (_hotbar[i] == null)
			{
				_hotbar[i] = item;
				EmitSignal(SignalName.InventoryChanged); 
				EmitSignal(SignalName.SlotSelected, i);
				return true; 
			}
		}
		return false; 
	}


	/// <summary>
	/// Finds next open (empty) slot in inventory 
	/// </summary>
	public int NextEmpty()
	{
		for (int i = 0; i < HotbarSize; i++)
		{
			if (_hotbar[i] == null)
			{
				return i; 
			}
		}
		return -1; 
	}


	/// <summary>
	/// Given param index, this will change the current selected slot to be the index and emit signal. 
	/// </summary>
	public void SelectSlot(int index)
	{
		//GD.Print(index); 
		_selectedSlot = Mathf.Clamp(index, 0, HotbarSize - 1);
		EmitSignal(SignalName.SlotSelected, _selectedSlot);
	}


	/// <summary>
	/// Sends out signal so item is spawned in from the Player.drop_from_player() function. 
	/// </summary>
	public void spawn_item(ItemData item)
	{
		// Don't instantiate here. Player.drop_from_player() is the single
		// place that spawns + positions the dropped item — instantiating a
		// second copy here (with no position set) was the source of the
		// "always drops in the same spot" bug.
		EmitSignal(SignalName.ItemDrop, item);
	}


	/// <summary>
	/// Given the slot of a Polaroid Photo, this will send signal that a photoo is developing, at which index, and for how long.
	/// Used in the PolaroidCamera.cs file.
	/// </summary>
	public void NotifyPhotoDeveloping(int slotIndex, Texture2D texture, float duration)
	{
		EmitSignal(SignalName.PhotoDeveloping, slotIndex, texture, duration);
	}


	/// <summary>
	/// drops item at index slotIndex after g is pressed.
	/// </summary>
	public void drop_item(int slotIndex)
	{
		if (_hotbar[slotIndex] != null)
		{
			ItemData dropped_item = _hotbar[slotIndex]; // Grab Item
			spawn_item(dropped_item); // This will now signal to the Player.cs file that item dropped at slotINdex needs to spawn relative to player pos 
			_hotbar[slotIndex] = null; // Empty the current slot
			EmitSignal(SignalName.InventoryChanged); 
			if (slotIndex == _selectedSlot)
				EmitSignal(SignalName.SlotSelected, _selectedSlot);
		}
	}


	/// <summary>
	/// Similar to drop_item, but no world object gets spawned.
	/// Used when an item is consumed (eaten, used up) rather than dropped.
	/// </summary>
	public void despawn_item(int slotIndex)
	{
		if (_hotbar[slotIndex] != null)
		{
			_hotbar[slotIndex] = null;
			EmitSignal(SignalName.InventoryChanged);
			if (slotIndex == _selectedSlot)
				EmitSignal(SignalName.SlotSelected, _selectedSlot);
		}
	}
 

	/// <summary>
	/// Calls the .Use function of the item (every item has this function) and despawns item.
	/// </summary>
	public void UseSelectedItem(Player player)
	{
		//GD.Print("THIS GOT CALLED");
		ItemData item = _hotbar[_selectedSlot];

		
		if (item == null) {GD.Print("IT'S NULL"); return;}
 
		if(item.isConsumable == true)
		{
			bool consumed = item.Use(player);
			if (consumed)
			{
				//GD.Print("HELLO CONSUMED");
				despawn_item(_selectedSlot);
			}
		}
		else
		{
			item.Use(player);
		}
		
	}
}
