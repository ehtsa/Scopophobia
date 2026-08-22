using Godot;
using System;

[GlobalClass]
public partial class ItemData : Resource
{
	[Export] public String item_name = ""; 
	[Export] public Texture2D icon { get; set;} = GD.Load<Texture2D>("res://Assets/icon.svg"); // May need to change path to rse:/assets/icon.svg or smth.
	[Export] public PackedScene MeshScene { get; set; }
	[Export] public PackedScene InteractableScene { get; set; } = GD.Load<PackedScene>("res://Scenes/interactables.tscn");
	[Export] public TextLine description; 
	[Export] public bool isConsumable = true; 

	public virtual bool Use(Player player)
	{
		GD.Print(item_name + " has no defined use.");
		return false; 
	}

	
}
