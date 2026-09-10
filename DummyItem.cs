using Godot;
using System;
using System.Threading.Tasks;

public partial class DummyItem : StaticBody3D
{
	private AudioStreamPlayer _pickupSound;
	private Node3D _model; 
	private CollisionShape3D _collisionLayer;
	public TextureRect _inHandModel; 

	[Export] public Main mainNode; 
	[Export] public Player playerNode; 
	

	public override void _Ready()
	{
		mainNode = GetNode<Main>("../");
		playerNode = GetNode<Player>("../Player");
		_pickupSound = GetNode<AudioStreamPlayer>("PickupSound");
		_model = GetNode<Node3D>("Model");
		_collisionLayer = GetNode<CollisionShape3D>("CollisionLayer");
		_inHandModel = GetNode<TextureRect>("InHandModel");
		_inHandModel.Hide();
	}

	public async void Interact()
	{
		_pickupSound.Play();
		Hide(); // So it gives it the illusion it disappears
		this.CollisionLayer = 0; // The following make it so it removes any collision while the sound is still being ran 
		this.CollisionMask = 0;

		TextureRect newItemForPlayer = (TextureRect)_inHandModel.Duplicate();
		// 2. Make it visible (since it was hidden in _Ready)
		newItemForPlayer.Show();

		playerNode.AddChild(newItemForPlayer);
		// 4. Update the player's internal state
		//playerNode.isHoldingItem = true;

		await ToSignal(_pickupSound, AudioStreamPlayer.SignalName.Finished);
		this.QueueFree();
	}

}
