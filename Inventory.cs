using Godot;
using System;

/// <summary>
/// Inventory Class. Holds each slot and data for each item held in inventory. 
/// </summary>
public partial class Inventory : Node 
{
	// 2. Signals are defined using delegates and the [Signal] attribute.
	// Note: The delegate name MUST end with "EventHandler".
	[Signal] public delegate void InventoryChangedEventHandler(); 
	[Signal] public delegate void SlotSelectedEventHandler(int slotIndex);
	[Signal] public delegate void ItemDropEventHandler(ItemData item);
	[Signal] public delegate void PhotoDevelopingEventHandler(int slotIndex, Texture2D texture, float duration);

	public const int HotbarSize = 5; 
	public ItemData[] _hotbar = new ItemData[HotbarSize]; 
	public int _selectedSlot = 0; 

	public void _input(InputEvent @event)
	{
		if(Input.IsActionJustPressed("press_g"))
		{
			drop_item(_selectedSlot);
		}
	}

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

	public void SelectSlot(int index)
	{
		//GD.Print(index); 
		_selectedSlot = Mathf.Clamp(index, 0, HotbarSize - 1);
		EmitSignal(SignalName.SlotSelected, _selectedSlot);
	}

	public void spawn_item(ItemData item)
	{
		// Don't instantiate here. Player.drop_from_player() is the single
		// place that spawns + positions the dropped item — instantiating a
		// second copy here (with no position set) was the source of the
		// "always drops in the same spot" bug.
		EmitSignal(SignalName.ItemDrop, item);
	}

	// In your Inventory or UI script
	// public void spawn_item(ItemData item)
	// {
	// 	// Remove the instantiation completely. Just emit the signal!
	// 	EmitSignal(SignalName.ItemDrop, item);
	// }

	public void NotifyPhotoDeveloping(int slotIndex, Texture2D texture, float duration)
	{
		EmitSignal(SignalName.PhotoDeveloping, slotIndex, texture, duration);
	}

	public void drop_item(int slotIndex)
	{
		if (_hotbar[slotIndex] != null)
		{
			ItemData dropped_item = _hotbar[slotIndex];
			spawn_item(dropped_item); 
			_hotbar[slotIndex] = null; 
			EmitSignal(SignalName.InventoryChanged);
			if (slotIndex == _selectedSlot)
				EmitSignal(SignalName.SlotSelected, _selectedSlot);
		}
	}

	// Same bookkeeping as drop_item, but no world object gets spawned.
	// Used when an item is consumed (eaten, used up) rather than dropped.
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
 
	// Call this from wherever your "use item" input is handled.
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
