using Godot;
using System;

/// <summary>
/// This is the Main Node Class, used as the Main World where things can spawn in. 
/// </summary>
public partial class Main : Node
{
	// Called when the node enters the scene tree for the first time.
	[Export] public PackedScene MobScene { get; set; }
	[Export] public SpawnArea _spawnArea; // NOTE: Used to spawn pills randomly. Use this template in the future for spawning things. 
	[Export] private Control _pauseMenu; 
	[Export] private AudioStreamPlayer _openMenuSound; 
	[Export] private AudioStreamPlayer _closeMenuSound;
	[Export] private AudioStreamPlayer _eerieMusic; 

	private bool debug = true; // Added during initial development stage. First used to prevent automatic mob spawning.  
	public bool paused = false; // Used to check if game is paused. 

	/// <summary>
	/// The Ready function of the Main Node. 
	/// </summary>
	/// <remarks>
	/// NOTE: Ready functions are always ran at the top of the Node Initialization. 
	/// Each GetNode...("Name") initializes the instance variables used throughout the rest of this code
	/// </remarks> 
	public override void _Ready()
	{
		_pauseMenu = GetNode<Control>("PauseMenu");
		Input.MouseMode = Input.MouseModeEnum.Captured; 
		_pauseMenu.Hide();
		
		_openMenuSound = GetNode<AudioStreamPlayer>("OpenMenuSound");
		_closeMenuSound = GetNode<AudioStreamPlayer>("CloseMenuSound");
		_eerieMusic = GetNode<AudioStreamPlayer>("EerieMusic");
		_spawnArea = GetNode<SpawnArea>("SpawnArea");
		PlayBackgroundMusic(); 
	}

	/// <summary>
	/// Plays Background Music. 
	/// </summary>
	public void PlayBackgroundMusic()
	{
		if (!_eerieMusic.Playing)
		{
			_eerieMusic.Play(); // Plays from the beginning or current position
		}
	}

	
	/// <summary>
	/// UnhandledInput for Main. Helps with Pause Menu (Pressing ESC) and for spawning the pills (press_o) in Spawn Area 
	/// </summary>
	/// <remarks>
	/// Please check the SpawnArea.cs file to see what SpawnRandomObject() does
	/// </remarks> 
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("press_escape"))
		{
			// GetTree().Quit(); // Exits the game when Escape is pressed. Must have an action named "press_escape" in the Input Map.
			show_pause_menu();
		}
		if (@event.IsActionPressed("press_o"))
		{
			_spawnArea.SpawnRandomObject();  // Check the SpawnArea.cs file 
		}
	}

	/// <summary>
	/// Opens the pauseMenu. Called by Unhandled Input.
	/// </summary>
	public void show_pause_menu()
	{
		paused = !paused;
		
		if(paused)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible; // Release mouse from being locked to center 
			_pauseMenu.Show();
			Engine.TimeScale = 0; // Stops anything from moving / doing anything
			_openMenuSound.Play();
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Captured; // Lock mouse back to center
			_pauseMenu.Hide(); 
			Engine.TimeScale = 1;
			_closeMenuSound.Play();
		}

		
	}

	/// <summary>
	/// Turns Debug Mode On. 
	/// </summary>
	public void debug_mode_on()
	{
		debug = !debug;
	}

	/// <summary>
	/// Spawns a "Mob" which is the current (8.23.26) name of the Weeping Angel mob code. 
	/// Will spawn in the Spawn Location area shown in the Main Tree. 
	/// </summary>
	/// <remarks>
	/// NOTE to future Conrad and Emi (as of 8.23.26) Please change the name of the Weeping Angel Mob Code, and this function.  
	/// </remarks>
	public void spawn_mob()
	{
		Mob mob = MobScene.Instantiate<Mob>();
		mob.AddToGroup("mobs"); // Adds the instantiated mob to a group called Mobs. See the Mob.cs to understand more. 
		var mobSpawnLocation = GetNode<PathFollow3D>("SpawnPath/SpawnLocation"); // The slash here is very much like directory slashes. SpawnLocation is in SpawnPath. 
		mobSpawnLocation.ProgressRatio = GD.Randf(); // A Random Location on the SpawnLocation Path. Think of Progress Ratio as a percentage on a line, where 1.0 is one end, and 0.0 is the other. 

		// Used GlobalPosition here instead of Position for safety!
		Vector3 playerPosition = GetNode<Player>("Player").GlobalPosition; 
		mob.Initialize(mobSpawnLocation.GlobalPosition, playerPosition); 
		AddChild(mob); // Spawns mob. 
	}

	/// <summary>
	/// Used in initial development. Spawns a stupid everytime the timer goes off. Can be deleted after initial development stage (as of 8.23.26)
	/// </summary>
	private void _on_timer_timeout()
	{
		if (!debug){
			Mob mob = MobScene.Instantiate<Mob>();

			var mobSpawnLocation = GetNode<PathFollow3D>("SpawnPath/SpawnLocation");
			mobSpawnLocation.ProgressRatio = GD.Randf();

			// Used GlobalPosition here instead of Position for safety!
			Vector3 playerPosition = GetNode<Player>("Player").GlobalPosition; 
			mob.Initialize(mobSpawnLocation.GlobalPosition, playerPosition); 
			AddChild(mob);
			
		}
		
	}

}
