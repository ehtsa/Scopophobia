using Godot;
using System;
using System.Collections.Generic;


/// <summary>
/// The Base Mob Class that accidentally turned into the Weeping Angel Code. Must change the name to Weeping Angel or something ilke that if you can. 
/// Also, We need to template this class such that we can use it as a "resource" in Godot. 
/// </summary>
public partial class Mob : CharacterBody3D
{
	[Export] public MobData Data { get; set; } // The basic information of this Mob. 
	[Export] public float RotationSpeed { get; set; } = 5f; // higher = snappier turn
	[Export] public NavigationAgent3D NavigationAgent { get; set; }
	[Export] public float VelocityChange { get; set; }
	[Export] public float MinFootstepInterval { get; set; } = 0.05f; // seconds between steps at max speed
	[Export] public float MaxFootstepInterval { get; set; } = 0.20f;  // seconds between steps at min speed
	[Export] public float VanishFadeDuration { get; set; } = 1.0f; // seconds for the mob to fade out

	[Export] public NodePath SmokeParticlesPath;
	[Export] public NodePath ModelMeshPath;

	private GpuParticles3D _smokeParticles;
	private Node3D _modelMesh;

	protected Player player;

	private AudioStreamPlayer spawnSting;
	private AudioStreamPlayer3D jumpscareSting;
	private AudioStreamPlayer3D sprintSound; 
	private float footstepTimer;
	private Vector3 lastPlayerPosition;
	private Vector3 lastEnemyPosition;
	private int randomSpeed; 

	private bool isSeen; 


	/// <summary>
	/// Ready function of the Mob Class. 
	/// </summary>
	public override void _Ready()
	{
		player = GetTree().GetFirstNodeInGroup("player") as Player;
		spawnSting = GetNode<AudioStreamPlayer>("SpawnSound");
		sprintSound = GetNode<AudioStreamPlayer3D>("SprintSound");
		jumpscareSting = GetNode<AudioStreamPlayer3D>("JumpScareSound");
		randomSpeed = GD.RandRange((int) Data.MinSpeed, (int) Data.MaxSpeed);

		// An Attempt at making the mob disappear after it was revealed that it is a mob. 
		if (SmokeParticlesPath != null)
			_smokeParticles = GetNode<GpuParticles3D>(SmokeParticlesPath);
			
		if (ModelMeshPath != null)
			_modelMesh = GetNode<Node3D>(ModelMeshPath);

		spawnSting.Play(); 
	}


	/// <summary>
	/// Puts mobs at level 1 if they are real, level 2 if they're fake.  
	/// Done so we don't have to manually put the levels. 
	/// </summary>
	private void ApplyRealityLayer()
	{
		// visualRoot = the MeshInstance3D / model root, not the CharacterBody3D itself
		MeshInstance3D visualRoot = GetNode<MeshInstance3D>("Pivot/Character");
		visualRoot.Layers = Data.isReal ? 1u : 2u;
		GD.Print(visualRoot);
	}


	/// <summary>
	/// Physics process (called every frame i think)
	/// </summary>
	/// <param name="delta"></param>
	public override void _PhysicsProcess(double delta)
	{
		Move(delta);
	}


	// AVOIDANCE IS ON AND WILL BE NEEDED TO ACCOUNT FOR: 
	// One thing to flag in case you hit it later: if you've turned on Avoidance on your 
	// NavigationAgent3D (for mobs steering around each other or obstacles), the recommended 
	// Godot 4 pattern is a bit different — you call NavigationAgent.SetVelocity(desiredVelocity) 
	// and then read the actual movement velocity back from the VelocityComputed signal, 
	// rather than using GetNextPathPosition() directly. Not needed if avoidance is off, but 
	// worth knowing if mobs start clipping through each other and you want them to route around one another.

	/// <summary>
	/// The main move function. Takes in delta for time. Follows the navigation path until it reaches the player. 
	/// </summary>
	/// <remarks>
	/// AVOIDANCE IS ON AND WILL BE NEEDED TO ACCOUNT FOR: 
	/// One thing to flag in case you hit it later: if you've turned on Avoidance on your 
	/// NavigationAgent3D (for mobs steering around each other or obstacles), the recommended 
	/// Godot 4 pattern is a bit different — you call NavigationAgent.SetVelocity(desiredVelocity) 
	/// and then read the actual movement velocity back from the VelocityComputed signal, 
	/// rather than using GetNextPathPosition() directly. Not needed if avoidance is off, but 
	/// worth knowing if mobs start clipping through each other and you want them to route around one another.
	/// </remarks>
	/// <param name="delta"></param>
	public virtual void Move(double delta)
	{
		if (player == null) // used for debugging
		{
			//GD.Print("You are NULL as FUCK.");
			return;
		}

		UpdateNavigationTarget();

		if (NavigationAgent.IsTargetReached())
		{
			this.Velocity = Vector3.Zero; // stands still 
			GD.Print("Boo"); // this is the super scary jumpscare indicator 
			jumpscareSting.Play(); // plays sound
			MoveAndSlide(); // maybe get rid of this ? 
			// Need to implement a jumpscare animation
			return;
		}

		// If the player isn't looking at the mob
		if (isSeen == false)
		{
			LookAtPlayer((float)delta); // look at them
			Vector3 desiredVelocity = (NavigationAgent.GetNextPathPosition() - GlobalPosition).Normalized() * randomSpeed; // randomized speed, in the direction of the navgiation agent
			Velocity = Velocity.Lerp(desiredVelocity, VelocityChange * (float)delta); // Velocity
			MoveAndSlide();
		}
		UpdateFootsteps(delta);
	}


	/// <summary>
	/// Default to nothing but maybe we'll use for animation
	/// </summary>
	/// <param name="player"></param>
	public virtual void Attack(Player player)
	{
		// default, or leave empty
	}


	/// <summary>
	/// Function to look at the player
	/// </summary>
	/// <param name="delta"></param>
	protected void LookAtPlayer(float delta)
	{
		//GD.Print("Ou shiii... I'm looking at you.");
		Vector3 targetPos = player.GlobalPosition;
		targetPos.Y = GlobalPosition.Y; // yaw only, no tilt

		if (targetPos.IsEqualApprox(GlobalPosition))
			return; // avoid undefined look direction if mob overlaps player

		Transform3D targetTransform = GlobalTransform.LookingAt(targetPos, Vector3.Up);
		Quaternion currentRot = GlobalTransform.Basis.GetRotationQuaternion();
		Quaternion targetRot = targetTransform.Basis.GetRotationQuaternion();

		Basis = new Basis(currentRot.Slerp(targetRot, RotationSpeed * delta));

		if (lastPlayerPosition.DistanceTo(player.GlobalPosition) > 0.5f || lastEnemyPosition.DistanceTo(GlobalPosition) > 0.5f)
		{
			lastPlayerPosition = player.GlobalPosition;
			lastEnemyPosition = GlobalPosition;
			NavigationAgent.TargetPosition = lastPlayerPosition;
		}

		Vector3 desiredVelocity = (NavigationAgent.GetNextPathPosition() - GlobalPosition).Normalized() * randomSpeed;
		this.Velocity = Velocity.Lerp(desiredVelocity, VelocityChange * (float)delta);
		MoveAndSlide();
	}


	/// <summary>
	/// Updates the NavigationAgent helper so the mob can have a clear path to the player. 
	/// </summary>
	private void UpdateNavigationTarget()
	{
		if (lastPlayerPosition.DistanceTo(player.GlobalPosition) > 0.5f || lastEnemyPosition.DistanceTo(GlobalPosition) > 0.5f)
		{
			lastPlayerPosition = player.GlobalPosition;
			lastEnemyPosition = GlobalPosition;
			NavigationAgent.TargetPosition = lastPlayerPosition;
		}
	}


	/// <summary>
	/// Plays the footstep sound if speed > 0 and footsteptimer went off. Uss footsteptimer for intervals between steps. 
	/// </summary>
	/// <param name="delta"></param>
	private void UpdateFootsteps(double delta)
	{
		float speed = Velocity.Length();

		if (speed < 0.1f)
		{
			footstepTimer = 0f; // so a step fires immediately once it starts moving again
			return;
		}

		footstepTimer -= (float)delta;
		if (footstepTimer <= 0f)
		{
			sprintSound.Play();

			float t = Mathf.InverseLerp(Data.MinSpeed, Data.MaxSpeed, speed);
			footstepTimer = Mathf.Lerp(MaxFootstepInterval, MinFootstepInterval, Mathf.Clamp(t, 0f, 1f));
		}
	}

	
	/// <summary>
	/// This function gets called by the main scene to initialize a mob. 
	/// Takes care of mob looking at the player the moment it spawns. 
	/// </summary>
	/// <param name="startPosition"></param>
	/// <param name="playerPosition"></param>
	public void Initialize(Vector3 startPosition, Vector3 playerPosition)
	{
		// We position the mob by placing it at startPosition
		// and rotate it towards playerPosition, so it looks at the player.
		LookAtFromPosition(startPosition, playerPosition, Vector3.Up);
		// Rotate this mob randomly within range of -45 and +45 degrees,
		// so that it doesn't move directly towards the player.
		RotateY((float)GD.RandRange(-Mathf.Pi / 4.0, Mathf.Pi / 4.0));

		// // We calculate a random speed (integer).
		// int randomSpeed = GD.RandRange((int) Data.MinSpeed, (int) Data.MaxSpeed);
		// // We calculate a forward velocity that represents the speed.
		// Velocity = Vector3.Forward * randomSpeed;
		// // We then rotate the velocity vector based on the mob's Y rotation
		// // in order to move in the direction the mob is looking.
		// Velocity = Velocity.Rotated(Vector3.Up, Rotation.Y);
	}


	/// <summary>
	/// Failed method. Hoping to get the mob to vanish with smoke but it won't work
	/// </summary>
	public void TriggerVanish()
	{
		// Fade every mesh part under ModelMeshPath to nothing over VanishFadeDuration, then free.
		// Walking the subtree means this works whether ModelMeshPath points straight at the mesh,
		// a rig with several mesh pieces, or a wrapper/pivot node above the actual mesh.
		
		MeshInstance3D visualRoot = GetNode<MeshInstance3D>("Pivot/Character");
		
		Tween tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(visualRoot, "transparency", 2.0f, VanishFadeDuration)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.In);
		tween.Chain().TweenCallback(Callable.From(QueueFree));
		
	}


	/// <summary>
	/// Grabs all mesh instances, i think used for deletion
	/// </summary>
	/// <param name="root"></param>
	/// <param name="results"></param>
	private static void CollectMeshInstances(Node3D root, List<GeometryInstance3D> results)
	{
		if (root == null)
			return;

		if (root is MeshInstance3D meshInstance)
			results.Add(meshInstance);

		foreach (Node child in root.GetChildren())
		{
			if (child is Node3D child3D)
				CollectMeshInstances(child3D, results);
		}
	}


	/// <summary>
	/// A connected signal that signifies when the mob is off the screen. 
	/// </summary>
	private void _on_visible_on_screen_notifier_3d_screen_exited()
	{
		GD.Print("hehe i'm creeping on you...");
		isSeen = false; 

	}	


	/// <summary>
	/// A connected signal that signifies when the mob is on the screen. Makes mob movement 0. 
	/// </summary>
	private void _on_visible_on_screen_notifier_3d_screen_entered()
	{
		GD.Print("oh shi Why you looking at me cuh.");
		// We calculate a forward velocity that represents the speed.
		isSeen = true; 
		Velocity = Vector3.Zero;
	}

	/// <summary>
	/// Signal for when player collision area enters the mob area. 
	/// </summary>
	/// <param name="node"></param>
	private void _on_area_3d_body_entered(Node3D node)
	{
		if (node == player)
		{
			GD.Print("Boo");
			jumpscareSting.Play(); 
			if (!Data.isReal)
			{
				player.SetMentalHealth(-20);
				TriggerVanish(); 
			}
			
		}
	}


	/// <summary>
	/// TriggersVanish when photographed. I believe this is also failed method. 
	/// </summary>
	public virtual void OnPhotographed()
	{
		GD.Print("TRIGGERING VANISH BITCH");
		TriggerVanish(); // fade out rather than an instant disappearance. Override per subclass for something else entirely.
	}
}
