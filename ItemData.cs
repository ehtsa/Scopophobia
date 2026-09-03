using Godot;
using System;

/// <summary>
/// The parent class of all Items (pills, camera, etc.)
/// </summary>
[GlobalClass]
public partial class ItemData : Resource
{
	[Export] public String item_name = ""; // Name. Used particular for Camera baed on items to check if they are a camera
	[Export] public Texture2D icon { get; set;} = GD.Load<Texture2D>("res://Assets/icon.svg"); // The icon to be shown in the hot bar.
	[Export] public PackedScene MeshScene { get; set; } // The mesh scene to be shown in the hand. 
	[Export] public PackedScene InteractableScene { get; set; } = GD.Load<PackedScene>("res://Scenes/interactables.tscn"); // The physical item 
	[Export] public TextLine description; 
	[Export] public bool isConsumable = true; // true for things like pills, false for things like cameras.

	/// <summary>
	/// A required function whose existence is checked by the Player code to affirm it's an item. 
	/// Based function does nothing. 
	/// </summary>
	public virtual bool Use(Player player)
	{
		GD.Print(item_name + " has no defined use.");
		return false; 
	}

	
}
