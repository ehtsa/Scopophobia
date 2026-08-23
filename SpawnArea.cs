using Godot;
using System;

/// <summary>
/// This is the SpawnArea class, used for spawning things in a randomly generated coordinate within the space, as seen in Main.cs
/// </summary>
public partial class SpawnArea : Area3D
{
	// Drag your object scene (e.g., enemy.tscn) into this slot in the Inspector
	// NOTE: Anything with Export will be shown in the Godot Editor as a new item on the right. 
	// For every node of SpawnArea, a new object to spawn will need to be dragged into this field. 
	[Export] public PackedScene ObjectToSpawn { get; set; }

	private CollisionShape3D _spawnArea;

	/// <summary>
	/// The Ready function of the Main Node. 
	/// </summary>
	/// <remarks>
	/// NOTE: Ready functions are always ran at the top of the Node Initialization. 
	/// Each GetNode...("Name") initializes the instance variables used throughout the rest of this code
	/// </remarks> 
	public override void _Ready()
	{
		// Cache the reference to the collision shape node
		_spawnArea = GetNode<CollisionShape3D>("Area");
	}

	/// <summary>
	/// Spawns random object by taking spawnArea shape, and generates a random X, Y, Z Coordinate set for the ObjectToSpawn to be spawned at. 
	/// </summary>
	public void SpawnRandomObject()
	{
		// Safety check to ensure a scene is assigned
		if (ObjectToSpawn == null)
		{
			GD.PrintErr("Missing ObjectToSpawn scene assignment!");
			return;
		}

		// 1. Cast the shape to BoxShape3D and calculate extents (half-sizes)
		if (_spawnArea.Shape is BoxShape3D boxShape)
		{
			Vector3 extents = boxShape.Size / 2;

			// 2. Generate random offsets within the positive and negative extents
			float randomX = (float)GD.RandRange(-extents.X, extents.X);
			float randomY = (float)GD.RandRange(-extents.Y, extents.Y);
			float randomZ = (float)GD.RandRange(-extents.Z, extents.Z);
			Vector3 randomOffset = new Vector3(randomX, randomY, randomZ);

			// 3. Combine the zone's absolute position with our calculated offset
			Vector3 finalPosition = _spawnArea.GlobalPosition + randomOffset;

			// 4. Instantiate the object and assign its world coordinates
			// 1. Instantiate the object in memory
			Node3D spawnedInstance = ObjectToSpawn.Instantiate<Node3D>();

	// 2. ADD IT TO THE TREE FIRST (Fixes the !is_inside_tree() error)
			GetParent().AddChild(spawnedInstance);

			// 3. SET THE GLOBAL POSITION SECOND
			spawnedInstance.GlobalPosition = finalPosition;
		}
		else
		{
			GD.PrintErr("CollisionShape3D must be a BoxShape3D!");
		}
	}
}
