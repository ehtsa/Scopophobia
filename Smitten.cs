using Godot;

/// <summary>
/// Smitten: stays completely frozen and silent while the player can see it (on-screen).
/// The moment it drops off-screen it starts stalking - a 20-second timer counts down while a
/// looping stalking sound plays. If the player looks back at it before the timer runs out, the
/// timer resets and the stalking sound stops, same as if nothing happened. If the timer finishes
/// while it's still unseen, it commits to the chase: it charges straight at the player and can no
/// longer be frozen by being seen again. Reaching the player (Area3D contact, not nav-agent
/// distance - this project moved off IsTargetReached() for proximity a while back) fires the
/// jumpscare via the inherited Attack() hook.
/// </summary>
public partial class Smitten : Mob
{
	private enum SmittenState
	{
		Idle,      // on-screen (or hasn't gone off-screen yet since the last reset) - frozen, silent
		Stalking,  // off-screen, stalk timer counting down
		Charging   // timer expired - committed, can't be frozen again
	}

	[Export] public float StalkDuration { get; set; } = 20f;
	[Export] public float ChargeSpeed { get; set; } = 8f;
	[Export] public float AttackDamage { get; set; } = -20f;

	// Own references rather than the base class's - Mob keeps its audio nodes private,
	// so Smitten grabs its own from the same scene tree.
	private AudioStreamPlayer3D _stalkingSound;
	private AudioStreamPlayer3D _jumpscareSound;

	private SmittenState _state = SmittenState.Idle;
	private float _stalkTimer;

	private Vector3 _lastPlayerPosition;
	private Vector3 _lastSelfPosition;

	public override void _Ready()
	{
		base._Ready(); // still grabs the player reference, plays the spawn sound, etc.

		// "StalkingSound" is a placeholder node name - add an AudioStreamPlayer3D with that
		// name (and a real stalking loop assigned) to the Smitten scene.
		_stalkingSound = GetNode<AudioStreamPlayer3D>("StalkingSound");
		_jumpscareSound = GetNode<AudioStreamPlayer3D>("JumpScareSound");
	}

	public override void Move(double delta)
	{
		if (player == null)
			return;

		switch (_state)
		{
			case SmittenState.Idle:
				Velocity = Vector3.Zero;
				break;

			case SmittenState.Stalking:
				_stalkTimer -= (float)delta;
				if (_stalkTimer <= 0f)
					StartCharging();
				break;

			case SmittenState.Charging:
				ChaseAndCharge(delta);
				break;
		}
	}

	/// <summary>
	/// Full-speed pursuit. No IsTargetReached() check here on purpose - this codebase already
	/// pivoted proximity detection to the Area3D/body_entered signal, so the actual "catch" is
	/// handled by _on_area_3d_body_entered below, not by nav-agent distance.
	/// </summary>
	private void ChaseAndCharge(double delta)
	{
		if (_lastPlayerPosition.DistanceTo(player.GlobalPosition) > 0.5f || _lastSelfPosition.DistanceTo(GlobalPosition) > 0.5f)
		{
			_lastPlayerPosition = player.GlobalPosition;
			_lastSelfPosition = GlobalPosition;
			NavigationAgent.TargetPosition = _lastPlayerPosition;
		}

		Vector3 desiredVelocity = (NavigationAgent.GetNextPathPosition() - GlobalPosition).Normalized() * ChargeSpeed;
		Velocity = Velocity.Lerp(desiredVelocity, VelocityChange * (float)delta);
		MoveAndSlide();
		FaceMovementDirection((float)delta);
	}

	/// <summary>
	/// Yaw-only turn to face the direction it's currently running, same quaternion-slerp
	/// approach the base class uses for facing the player.
	/// </summary>
	private void FaceMovementDirection(float delta)
	{
		Vector3 flatVelocity = Velocity with { Y = 0 };
		if (flatVelocity.LengthSquared() < 0.01f)
			return;

		Transform3D targetTransform = GlobalTransform.LookingAt(GlobalPosition + flatVelocity, Vector3.Up);
		Quaternion currentRot = GlobalTransform.Basis.GetRotationQuaternion();
		Quaternion targetRot = targetTransform.Basis.GetRotationQuaternion();
		Basis = new Basis(currentRot.Slerp(targetRot, RotationSpeed * delta));
	}

	private void StartStalking()
	{
		if (_state != SmittenState.Idle)
			return; // already stalking, or already charging and can't be un-committed

		_state = SmittenState.Stalking;
		_stalkTimer = StalkDuration;
		_stalkingSound.Play();
		GD.Print("Smitten: off-screen, stalk timer started.");
	}

	private void StartCharging()
	{
		_state = SmittenState.Charging;
		_stalkingSound.Stop();
		GD.Print("Smitten: timer expired, charging.");
	}

	private void ResetToIdle()
	{
		if (_state == SmittenState.Charging)
			return; // too late - it's already coming, being seen doesn't stop it anymore

		_state = SmittenState.Idle;
		_stalkTimer = StalkDuration;
		_stalkingSound.Stop();
	}

	/// <summary>
	/// It left the camera frustum - start (or continue) stalking.
	/// </summary>
	private void _on_visible_on_screen_notifier_3d_screen_exited()
	{
		StartStalking();
	}

	/// <summary>
	/// Player is looking at it again - freeze and reset the stalk timer, unless it's already charging.
	/// </summary>
	private void _on_visible_on_screen_notifier_3d_screen_entered()
	{
		ResetToIdle();
	}

	/// <summary>
	/// Actual physical contact with the player during the charge.
	/// </summary>
	private void _on_area_3d_body_entered(Node3D node)
	{
		if (node == player && _state == SmittenState.Charging)
			Attack(player);
	}

	/// <summary>
	/// The catch. Same mental-health hit as the base template's `_on_area_3d_body_entered`.
	/// Only vanishes afterward if this instance turned out to be a hallucination (isReal ==
	/// false) - a real one stays put once it's caught you.
	/// </summary>
	public override void Attack(Player player)
	{
		Velocity = Vector3.Zero;
		_jumpscareSound.Play();

		player.SetMentalHealth(AttackDamage);

		if (!Data.isReal)
			TriggerVanish();
	}
}
