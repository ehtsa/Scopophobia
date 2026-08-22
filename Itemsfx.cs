 
using System;
using Godot;

// Standalone helper — not tied to Player at all. Any ItemData subclass just
// needs a Node that's currently in the tree to hang a temporary
// AudioStreamPlayer off of, and player (passed into Use()) already is one.
public static class Itemsfx
{
	public static void PlayOneShot(Player parent, AudioStream stream)
	{
		if (stream == null || parent == null) return;
 
		AudioStreamPlayer oneShot = new AudioStreamPlayer();
		oneShot.Stream = stream;
		parent.AddChild(oneShot);
		oneShot.Play();
		oneShot.Finished += oneShot.QueueFree;
	}
}
 