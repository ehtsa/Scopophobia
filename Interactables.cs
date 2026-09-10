using Godot;
using System;

/// <summary>
/// THis is the class that helps us spawn in our items in the in-game world. 
/// </summary>
public partial class Interactables : RigidBody3D
{
	// 1. @onready in C# is typically assigned inside the _Ready() method.
	private MeshInstance3D _meshInstanceNode;

	// 2. @export is replaced by the [Export] attribute on a property.
	[Export] public ItemData ItemData { get; set; }
	[Export] public AudioStreamPlayer ItemGet;

	/// <summary>
	/// Ready function of the interactable class. 
	/// </summary>
	public override void _Ready()
	{
		// Equivalent to: @onready var mesh_instance_node = $MeshInstance3D
		_meshInstanceNode = GetNode<MeshInstance3D>("MeshInstance3D");
		ItemGet = GetNode<AudioStreamPlayer>("ItemGet");

		// It is good practice in C# to check if your exported resources are assigned
		if (ItemData != null)
		{
			Name = ItemData.item_name; 
			SpawnItemWithCollision(ItemData.MeshScene);
		}
		else
		{
			GD.PrintErr("ItemData is missing on " + Name);
		}
	}

	/// <summary>
	/// Used for when Player RayCast hovers over item in-game. Adds to inventory if interacted with. 
	/// </summary>
	public void Interact()
	{
		// 3. Fetching the Inventory Autoload (Singleton)
		Inventory inventory = GetNode<Inventory>("/root/Inventory");

		if (inventory.AddItem(ItemData))
		{
			// 4. call_deferred in C# uses the built-in MethodName cache
			ItemGet.Play(); 
			CallDeferred(MethodName.QueueFree);
		}
		else
		{
			GD.Print("Full Inventory");
		}
	}

	/// <summary>
	/// . C# requires strict typing, so we specify 'Node' as the argument 
	/// and 'MeshInstance3D' as the return type.
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	private MeshInstance3D FindFirstMeshInstance(Node node)
	{
		// Pattern matching in C# makes checking types very clean
		if (node is MeshInstance3D meshInstance)
		{
			return meshInstance;
		}

		foreach (Node child in node.GetChildren())
		{
			MeshInstance3D result = FindFirstMeshInstance(child);
			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	/// This spawns item in the world with collision.
	private Node3D SpawnItemWithCollision(PackedScene scene)
	{
		if (scene == null) return null;

		// Instantiate needs a generic type so Godot knows what kind of node it's returning
		Node3D inst = scene.Instantiate<Node3D>();

		MeshInstance3D meshInstance = FindFirstMeshInstance(inst);
		
		if (meshInstance != null && meshInstance.Mesh != null)
		{
			// 7. Creating new nodes in C# is done with the 'new' keyword
			Shape3D shape = meshInstance.Mesh.CreateConvexShape();
			CollisionShape3D colShape = new CollisionShape3D();
			colShape.Shape = shape;

			AddChild(inst);
			AddChild(colShape);
		}
		else 
		{
			// Fallback just to be safe
			AddChild(inst);
		}

		return inst;
	}
}
