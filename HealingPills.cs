using System;
using Godot;

/// <summary>
/// This is the HealingPills class, a subclass of ItemData. 
/// </summary>
[GlobalClass]
public partial class HealingPills : ItemData
{
	[Export] public float healthGain = 5.0f; // Will be randomized later 
	[Export] public AudioStream itemUseSound { get; set; } = GD.Load<AudioStream>("res://Assets/PillSwallow.mp3");
	

	/// <summary>
	/// This will allow the player to get healed for a random int from 5-10
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	public override bool Use(Player player)
	{
		Random rand = new Random(); 
		healthGain = rand.Next(5, 11); 

		player.SetMentalHealth(healthGain);
		Itemsfx.PlayOneShot(player, itemUseSound);

		return true; // consumed after use — Inventory handles removing it from the hotbar
	}
}
