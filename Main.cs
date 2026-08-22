using Godot;
using System;

public partial class Main : Node
{
	// Called when the node enters the scene tree for the first time.
	[Export] public PackedScene MobScene { get; set; }
	[Export] public PackedScene PillScene {get; set; }
	[Export] public SpawnArea _spawnArea;
	[Export] private Control _pauseMenu;
	[Export] private AudioStreamPlayer _openMenuSound; 
	[Export] private AudioStreamPlayer _closeMenuSound;
	[Export] private AudioStreamPlayer _eerieMusic; 

	private bool debug = true; // if true, things will stop spawning. 
	public bool paused = false; 

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

	public void PlayBackgroundMusic()
	{
		if (!_eerieMusic.Playing)
		{
			_eerieMusic.Play(); // Plays from the beginning or current position
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("press_escape"))
		{
			// GetTree().Quit(); // Exits the game when Escape is pressed. Must have an action named "press_escape" in the Input Map.
			show_pause_menu();
		}
		if (@event.IsActionPressed("press_o"))
		{
			_spawnArea.SpawnRandomObject(); 
		}
	}

	public void show_pause_menu()
	{
		paused = !paused;
		
		if(paused)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible; // Release mouse
			_pauseMenu.Show();
			Engine.TimeScale = 0;
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

	public void debug_mode_on()
	{
		debug = !debug;
	}

	public void spawn_mob()
	{
		Mob mob = MobScene.Instantiate<Mob>();
		mob.AddToGroup("mobs");
		var mobSpawnLocation = GetNode<PathFollow3D>("SpawnPath/SpawnLocation");
		mobSpawnLocation.ProgressRatio = GD.Randf();

		// Used GlobalPosition here instead of Position for safety!
		Vector3 playerPosition = GetNode<Player>("Player").GlobalPosition; 
		mob.Initialize(mobSpawnLocation.GlobalPosition, playerPosition); 
		AddChild(mob);
	}

	// We also specified this function name in PascalCase in the editor's connection window.
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
