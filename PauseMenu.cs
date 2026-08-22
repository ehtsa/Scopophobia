using Godot;
using System;

public partial class PauseMenu : Control
{
	// 1. Changed the type from 'Node' to 'Main'
	// (I also renamed the variable to 'MainNode' to avoid confusing the compiler with the class name 'Main')
	[Export] public Main MainNode;
	[Export] private AudioStreamPlayer _hoverSound; 

	public override void _Ready()
	{
		// 2. We cast this as <Main> instead of <Node>
		MainNode = GetNode<Main>("../"); // We use ../ to go up a parent node. if we do ../../, it would over shoot outside of main since this node is a child of the Main Node. 
		_hoverSound = GetNode<AudioStreamPlayer>("HoverSound");
	}
	
	private void _on_resume_pressed()
	{
		// 3. Now C# knows this is the Main script, and allows your custom method!
		MainNode.show_pause_menu();
	}
	
	private void _on_resume_mouse_entered()
	{
		// add in sounds
		_hoverSound.Play(); 
	}
	
	private void _on_settings_pressed()
	{
		
	}

	private void _on_settings_mouse_entered()
	{
		// add in sounds
		_hoverSound.Play();
	}

	private void _on_quit_pressed()
	{
		GetTree().Quit(); 
	}

	private void _on_quit_mouse_entered()
	{
		// add in sounds
		_hoverSound.Play();
	}
}
