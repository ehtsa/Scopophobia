using Godot;
using System;

public partial class SpawnArea : Area3D
{
	// Drag your object scene (e.g., enemy.tscn) into this slot in the Inspector
	[Export] public PackedScene ObjectToSpawn { get; set; }

	private CollisionShape3D _spawnArea;

	public override void _Ready()
	{
		// Cache the reference to the collision shape node
		_spawnArea = GetNode<CollisionShape3D>("Area");
	}

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
