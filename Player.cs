using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Transactions;
using Godot;
using Microsoft.VisualBasic;

/// <summary>
/// This is the Player Class where things like the Controls to Player Stats are controlled. 
/// </summary>
public partial class Player : CharacterBody3D
{
	// ALL SOUNDS, grabbed in the ready function of each item --------------------------------------------------------------------------------
	private AudioStreamPlayer _walkingSound; 
	private AudioStreamPlayer _runningSound; 
	private AudioStreamPlayer _windedSound; 
	private AudioStreamPlayer _breathingSound;
	private AudioStreamPlayer _windedClearSound; 
	
	// PLAYER MOVEMENT and Crouching Instance Variables --------------------------------------------------------------------------------
	[Export] public int Speed { get; set; } = 10;
	[Export] public int CrouchSpeed { get; set; } = 5; 
			public int CrouchActionSpeed = 20; 
			private float default_height = 4.0f; 
			private float crouch_height = 1.5f;
			private CollisionShape3D _collisionShape; 
			private bool _isCrouched = false; 
			private float _crouchValue = 2.0f; 
	[Export] public int FallAcceleration { get; set; } = 75; // The downward acceleration when in the air, in meters per second squared.
	[Export] public float JumpVelocity = 20f;

	// SPRINTING Instance Variables --------------------------------------------------------------------------------
	[Export] public float SprintMultiplier { get; set; } = 1.5f; // Added for cleaner scaling
	private ColorRect _staminaBar;
	private float _staminaBarMaxWidth;
	private float _staminaBarOriginalX; // NEW: To remember where the center is
	private bool isSprinting = false; // To check if sprinting
	private bool isWinded = false; // To check if we've reach 0 sprinting power and need to recharge
	private float maxStamina = 100.0f; 
	private float stamina = 100.0f; 
	private float drain = 10.0f; 
	private float regain = 5.0f;

	// FIRST-PERSON Instance Variables--------------------------------------------------------------------------------
	private Node3D _head; // Parent of all Player Camera (not item) things
			[Export] public float MouseSensitivity { get; set; } = 0.002f;
			[Export] private Camera3D _camera;
			private float _cameraPitch = 0f; 
			private const float BASE_FOV = 75.0f; 
			private const float SPRINT_FOV  = 2.5f; 
			private Vector3 _targetVelocity = Vector3.Zero;

			public const float _BOB_FREQ = 0.5f; // Player Head bobbing 
			public const float _BOB_AMP = 0.08f;
			public float t_bob = 0.0f;

			private RayCast3D _seeCast; // Used to see if any interactable is in front of current player. 
			private GodotObject _lastInteractTarget = null;
			private bool _lastTargetInteractable = false;
			private Label _interactLabel;

	// POLAROID CAMERA RELATED Instance Variables ------------------------------------------------------------------------------
	[Export] private Camera3D _PhotoCamera; 
	[Export] public AnimationPlayer _zoomAnimation; // Zoom Animation used for PhotoCamera
	private bool _isZoomed = false; 
	
	// LEANING Instance Variables ------------------------------------------------------------------------------
	private float lean_angle =  15.0f; 
	private float lean_offset = 1.0f; 
	public float lean_speed  = 8.0f; 
	public float target_lean = 0.0f; // -1 is left, 0 is normal, 1 is right 
	public float current_lean = 0.0f; 
	
	// HEALTH Rules ---------------------------------------------------------------------------------
	[Export] private float MentalHealth { get; set; } = 100.0f; 
	private Label _mentalHealthLabel; 
	private float _currentDisplayedHealth = 100f;
	private Tween _healthTween; // Used for the animation of mental health going down 

	// MISC Instance Variables --------------------------------------------------------------------
	[Export] public Main _mainNode; // For accessing Spawn Function  
	[Export] public Inventory _inventory; // Player inventory 
	[Export] public Viewmodel _viewmodel; // The actual visible item shown
	public CanvasLayer _inventoryUI; 
	

	// ONE LINE FUNCTION ------------------------------------------------------------------------------------
	public void textureShow(TextureRect texture){texture.Show(); }
	public void textureHide(TextureRect texture){texture.Hide();}
	public Camera3D GetCamera3D() => _camera;
	public Camera3D GetPhotoCamera3D() => _PhotoCamera;

	/// <summary>
	/// As of 8.23.26, probably can be deleted. 
	/// </summary>
	public Node3D GetHeldItemMesh()
	{
		GD.Print(_viewmodel?.GetCurrentItemInstance());
		return _viewmodel?.GetCurrentItemInstance();
	} 


	/// <summary>
	/// Ready function of the Player File 
	/// </summary>
	public override void _Ready()
	{
		AddToGroup("player");
		// Fetch references to your Head and Camera nodes
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_PhotoCamera = GetNode<Camera3D>("Head/PolaroidPhotoRig/PhotoViewport/PhotoCamera");
		_seeCast = GetNode<RayCast3D>("Head/Camera3D/SeeCast");

		_walkingSound = GetNode<AudioStreamPlayer>("WalkingSound");
		_runningSound = GetNode<AudioStreamPlayer>("RunningSound");
		_breathingSound = GetNode<AudioStreamPlayer>("BreathingSound");
		_windedSound = GetNode<AudioStreamPlayer>("WindedSound");
		_windedClearSound = GetNode<AudioStreamPlayer>("WindedClearSound");
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");

		_zoomAnimation = GetNode<AnimationPlayer>("AnimationPlayer");
		
		_mainNode = GetNode<Main>("../");

		_staminaBar = GetNode<ColorRect>("CanvasLayer/StaminaBar"); // Make sure this path matches your scene!
		_staminaBarMaxWidth = _staminaBar.Size.X;
		_staminaBarMaxWidth = _staminaBar.Size.X;
		_staminaBarOriginalX = _staminaBar.Position.X; // NEW
		_mentalHealthLabel = GetNode<Label>("CanvasLayer/MentalHealthLabel");
		Color startingColor = _mentalHealthLabel.Modulate; 
		startingColor.A = 0f; 
		_mentalHealthLabel.Modulate = startingColor;

		MentalHealth = Mathf.Clamp(MentalHealth, 0.0f, 100.0f);

		_interactLabel = GetNode<Label>("CanvasLayer/BoxContainer/InteractLabel");

		_inventory = GetNode<Inventory>("/root/Inventory");
		_inventoryUI = GetNode<CanvasLayer>("/root/Inventory/UI");

        // 2. Connect the signal
        _inventory.ItemDrop += drop_from_player;
		UpdateHealthText(_currentDisplayedHealth);

		// Lock the mouse to the center of the screen
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}


	/// <summary>
	/// Checks if we can zoom, and uses the current isZoomed boolean to allow us to zoom.
	/// </summary>
	public void zoomChecker()
	{
		// Checks if the current item we have in the inventory is a Camera Type item. 
		ItemData camera = _inventory._hotbar[_inventory._selectedSlot];
		bool hasCamera = camera.item_name.Contains("Camera"); // How to Check if current Item is Camera 
		if (hasCamera)
		{
			if(!_isZoomed)
			{
				_zoomAnimation.Play("Zoom"); 
				MouseSensitivity = MouseSensitivity / 2; 
				_inventoryUI.Hide(); 
			}
			else
			{
				_zoomAnimation.PlayBackwards("Zoom"); 
				MouseSensitivity = MouseSensitivity * 2; 
				_inventoryUI.Show(); 
			}
			_isZoomed = !_isZoomed; 
		}
	}


	/// <summary>
	/// Sets the Mental Health Attribute and sets up the Animation for it to go down or up. 
	/// </summary>
	public void SetMentalHealth(float amount)
	{
		if (MentalHealth + amount > 100.0f)
		{
			MentalHealth = 100.0f; 
		}
		else if (MentalHealth + amount < 0.0f)
		{
			MentalHealth = 0.0f; 
		}
		else
		{
			MentalHealth += amount;
		}
		

		// 1. If a tween is already running (e.g., they took damage twice very fast), kill it
		if (_healthTween != null && _healthTween.IsValid())
		{
			_healthTween.Kill();
		}

		// 2. Create a new Tween attached to this node
		_healthTween = CreateTween();

		// 3. (Optional) Make the animation look smoother! 
		// Sine + Out makes the numbers rush fast at first, then slow down right as they hit the target.
		_healthTween.SetTrans(Tween.TransitionType.Sine);
		_healthTween.SetEase(Tween.EaseType.Out);

		// FADE IN
		_healthTween.SetParallel(true);

		// Fade IN the alpha channel ("modulate:a") to 1.0 (fully visible) over 0.2 seconds
		_healthTween.TweenProperty(_mentalHealthLabel, "modulate:a", 1.0f, 0.2f);

		// 4. TweenMethod smoothly calls our 'UpdateHealthText' function 
		// It goes from our CURRENT displayed health to the NEW target health, over 0.5 seconds.
		_healthTween.TweenMethod(Callable.From<float>(UpdateHealthText), _currentDisplayedHealth, MentalHealth, 0.5f);
	
		// FADE IN
		_healthTween.Chain(); 

		// FADE OUT
		_healthTween.TweenProperty(_mentalHealthLabel, "modulate:a", 0.0f, 3.0f);
	}


	/// <summary>
	/// updates the Mental Health Label; Called in SetMentalHealth. 
	/// </summary>
	private void UpdateHealthText(float value)
	{
		// Keep our tracking variable perfectly synced with the animation
		_currentDisplayedHealth = value;
		// Round the value so the player sees clean whole numbers (e.g., 85 instead of 85.3421)
		_mentalHealthLabel.Text = $"{Mathf.RoundToInt(value)} %";

	}
	

	/// <summary>
	/// Handles all unhandledInput. Just FYI: This will only run if an unhandled input is detect. Not ran at every frame, unlike _Process(). 
	/// </summary>
	public override void _UnhandledInput(InputEvent @event)
	{
		// For Mouse movement (Player Camera)
		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (@event is InputEventMouseMotion mouseMotion)
			{
				_head.RotateY(-mouseMotion.Relative.X*MouseSensitivity); 
				// Calculate the new pitch, add the mouse movement, and clamp it
				_cameraPitch -= mouseMotion.Relative.Y * MouseSensitivity;
				_cameraPitch = Mathf.Clamp(_cameraPitch, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));

				// Apply the clean, clamped pitch directly to the camera's rotation
				Vector3 cameraRotation = _camera.Rotation;
				cameraRotation.X = _cameraPitch;
				_camera.Rotation = cameraRotation;


				// OLD CODE HERE, can be deleted after Alpha build. Put here just in case (8.23.26)
				// _camera.RotateX(-mouseMotion.Relative.Y*MouseSensitivity);

                // Vector3 cameraRotation = _camera.Rotation;
                // cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-90f), Mathf.DegToRad(90f));
                // _camera.Rotation = cameraRotation;

				// Shake-to-develop-faster: while holding left click on a
				// not-yet-fully-developed Polaroid, vertical mouse motion
				// both shaves development time off (the real mechanic) and
				// wiggles the in-hand model (cosmetic only). Deliberately
				// NOT suppressing the look rotation above — you can still
				// look around while shaking. If you'd rather freeze the
				// camera while shaking, wrap the mouse-look block above in
				// `if (!isShakingPolaroid)` using the same condition below.
				if (Input.IsActionPressed("left_click") && _inventory._selectedSlot != -1
					&& _inventory._hotbar[_inventory._selectedSlot] is Polaroid shakingPolaroid
					&& !shakingPolaroid.IsFullyDeveloped)
				{
					shakingPolaroid.ApplyShakeMotion(mouseMotion.Relative.Y);
					_viewmodel?.ApplyShakeWobble(mouseMotion.Relative.Y);
					_viewmodel?.ApplyShakeWobble(mouseMotion.Relative.X);
				}
			}
		}

		// Handles any player controls like leaning, pressing sprint, etc. 
		if (!_mainNode.paused)
		{
			if (Input.IsActionJustPressed("press_shift") && !isWinded){	isSprinting = true; }
			if (Input.IsActionJustReleased("press_shift")){isSprinting = false; }
			if (Input.IsActionJustPressed("m_press")){SetMentalHealth(-5);} // Debug to show mental health goes down. 
			if (Input.IsActionJustPressed("p_press")){_mainNode.spawn_mob();} // spawns mob. 
			//if (Input.IsActionJustPressed("left_click")){GD.Print($"HELLO THE SELECTED SLOT IS {_inventory._selectedSlot}");}
			if (@event.IsActionPressed("left_click") && _inventory._selectedSlot != -1){GD.Print("USING THIS");_inventory.UseSelectedItem(this);}
			if (@event.IsActionPressed("right_click")){GD.Print("ZOOM IN ");zoomChecker();}
			if (@event.IsActionReleased("right_click")){GD.Print("ZOOM OUT");zoomChecker();}
			if (Input.IsActionPressed("left_lean")) {target_lean = -1.0f; }
			else if (Input.IsActionPressed("right_lean")) {target_lean = 1.0f; }
			else {target_lean = 0.0f;}
		}
	}


	/// <summary>
	/// Main Physics Process. Called every frame, so be careful with what you put in here. 
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{	
		// Code for being able to interact with items in this world. 
		_interactLabel.Hide();
		if (_seeCast.IsColliding())
		{
			var target = _seeCast.GetCollider();
			// HasMethod is a reflection-style lookup — only re-check it when
			// the thing you're looking at actually changes, not every tick
			// you happen to be staring at the same object.
			if (target != _lastInteractTarget)
			{
				_lastInteractTarget = target;
				_lastTargetInteractable = target != null && target.HasMethod("Interact");
			}
			if (_lastTargetInteractable)
			{
				_interactLabel.Show();
				if (Input.IsActionPressed("interact")) { target.Call("Interact"); }
			}
		}
		else
		{
			_lastInteractTarget = null;
		}

		// Leaning Logic 
		current_lean = Mathf.Lerp(current_lean, target_lean, (float)delta*lean_speed);
 
		float target_tilt = Mathf.DegToRad(-lean_angle * current_lean);
		float target_offset = lean_offset * current_lean; 
 
		// NOTE: The following code DOESN'T work because _camera.Rotation is NOT a variable. Can be deleted after Alpha (as of 8.23.26)
		//_camera.Rotation.Z = Mathf.Lerp(_camera.Rotation.Z, target_tilt, (float)delta*lean_speed); 
		//_camera.Position.X = Mathf.Lerp(_camera.Position.X, target_offset, (float)delta*lean_speed);
 
		// NOTE: Do the following code instead since we can make the _camera.Rotation be variables from here. 
		// 1. Store the current rotation and position in local variables
		Vector3 camRotation = _camera.Rotation;
		Vector3 camPosition = _camera.Position;
 
		// 2. Modify the specific axes on the local variables
		camRotation.Z = Mathf.Lerp(camRotation.Z, target_tilt, (float)delta * lean_speed); 
		camPosition.X = Mathf.Lerp(camPosition.X, target_offset, (float)delta * lean_speed);
 
		// 3. Assign the updated structs back to the camera
		_camera.Rotation = camRotation;
		_camera.Position = camPosition;
 
		CapsuleShape3D capsuleShape = (CapsuleShape3D)_collisionShape.Shape;
		bool wantsCrouch = Input.IsActionPressed("press_control");
		Speed = wantsCrouch ? CrouchSpeed : 10;
 
		// Only write to the physics shape while it's actually mid-transition.
		// Re-assigning .Height every tick — even to the same value — forces
		// the physics server to re-bake this shape, which is expensive and
		// was happening 60x/sec regardless of whether you were crouching.
		float targetHeight = wantsCrouch ? crouch_height : default_height;
		if (!Mathf.IsEqualApprox(capsuleShape.Height, targetHeight))
		{
			float newHeight = Mathf.MoveToward(capsuleShape.Height, targetHeight, CrouchActionSpeed * (float)delta);
			capsuleShape.Height = Mathf.Clamp(newHeight, crouch_height, default_height);
		}
 
		// Inputs of going in cardinal directions. 
		var direction = Vector3.Zero;
		if (Input.IsActionPressed("move_right"))   direction.X += 1.0f;
		if (Input.IsActionPressed("move_left"))    direction.X -= 1.0f;
		if (Input.IsActionPressed("move_back"))    direction.Z += 1.0f;
		if (Input.IsActionPressed("move_forward")) direction.Z -= 1.0f;
		

		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			// FIRST-PERSON FIX: Rotate the movement direction relative to the head's view direction.
			// This ensures pressing "W" moves you where you are looking, not just global North.
			direction = _head.GlobalTransform.Basis * direction;
		}
		
		// Handles the Sprint Mechanic 
		float currentSpeed = Speed;
		if (stamina <= 0f)
		{
			isWinded = true;
			isSprinting = false; 
			_windedSound.Play();
			_breathingSound.Play();
		}
		else if (isWinded && stamina >= 50f)
		{
			isWinded = false; 
			_windedClearSound.Play();
			_breathingSound.Stop();
		}
		
		if (isSprinting && !isWinded && direction != Vector3.Zero && stamina > 0f ) 
		{
			currentSpeed *= SprintMultiplier; 
			stamina -= drain * (float)delta;
		}
		else
		{
			if(stamina < maxStamina)
			{
				stamina += regain *(float)delta; 
			}
		}
		stamina = Mathf.Clamp(stamina, 0f, maxStamina);
		float targetAlpha = (stamina < maxStamina) ? 1.0f : 0.0f;
		// 2. Smoothly interpolate the current alpha towards the target alpha
		// The '10.0f' controls fade speed. Lower numbers = slower fade.
		Color currentModulate = _staminaBar.Modulate;
		currentModulate.A = Mathf.Lerp(currentModulate.A, targetAlpha, 10.0f * (float)delta);
		_staminaBar.Modulate = currentModulate;
 
		// 3. Only run the heavy math if the bar is actually somewhat visible
		if (currentModulate.A > 0.01f)
		{
			_staminaBar.Visible = true; 
			// --- CENTER-SHRINK LOGIC ---
			float staminaPercent = stamina / maxStamina;
			float currentWidth = staminaPercent * _staminaBarMaxWidth;
			// 1. Set the new shrunken width
			_staminaBar.Size = new Vector2(currentWidth, _staminaBar.Size.Y);
			// 2. Calculate how much width is missing
			float missingWidth = _staminaBarMaxWidth - currentWidth;
			// 3. Shift the bar to the right by exactly half of the missing width
			_staminaBar.Position = new Vector2(_staminaBarOriginalX + (missingWidth / 2f), _staminaBar.Position.Y);
			// --------------------------
			// Handle Color and Pulsing
			if (isWinded)
			{
				float time = Time.GetTicksMsec() / 1000f;
				float pulse = (Mathf.Sin(time * 8f) + 1f) / 2f; 
				
				Color darkRed = new Color(0.3f, 0f, 0f); 
				Color brightRed = new Color(0.8f, 0f, 0f); 
				
				_staminaBar.Color = darkRed.Lerp(brightRed, pulse);
			}
			else
			{
				_staminaBar.Color = Colors.White;
			}
		}
		else
		{
			_staminaBar.Visible = false; 
		}
 
		// IMPORTANT: ACTUAL MOVEMENT IS DONE HERE: 
		if (IsOnFloor())
		{
			if(direction != Vector3.Zero)
			{
				_targetVelocity.X = direction.X * currentSpeed;
				_targetVelocity.Z = direction.Z * currentSpeed;
				
			}
			else
			{
				_targetVelocity.X = Mathf.Lerp(_targetVelocity.X, direction.X * currentSpeed, (float)delta *7.0f);
				_targetVelocity.Z = Mathf.Lerp(_targetVelocity.Z, direction.Z * currentSpeed, (float)delta *7.0f);
			}
			if (Input.IsActionJustPressed("space") && !isWinded)
			{
				_targetVelocity.Y = JumpVelocity;
				stamina -= 15.0f;
			}
	else
	{
		// Optional: keeps the character grounded on slopes rather than bouncing
		_targetVelocity.Y = 0f; 
	}
		}
		else
		{
			_targetVelocity.Y -= FallAcceleration * (float)delta; // Gravity Falling 
			_targetVelocity.X = Mathf.Lerp(_targetVelocity.X, direction.X * currentSpeed, (float)delta *3.0f);
			_targetVelocity.Z = Mathf.Lerp(_targetVelocity.Z, direction.Z * currentSpeed, (float)delta *3.0f);
		}
		
		// Moving the character
		Velocity = _targetVelocity;
 
		float horizontalSpeed = new Vector2(Velocity.X, Velocity.Z).Length();
		// 2. Check if we are moving fast enough, on the floor, AND sprinting
		if (horizontalSpeed > 0.1f && IsOnFloor())
		{
			t_bob += (float)delta * horizontalSpeed;
			_camera.Transform = new Transform3D(_camera.Transform.Basis, _headbob(t_bob));
		}
		else
		{
			// 3. When walking or standing still, smoothly center the camera back to normal
			Vector3 defaultRestingPosition = new Vector3(0f, 0f, _camera.Position.Z);
			Vector3 smoothReturn = _camera.Position.Lerp(defaultRestingPosition, (float)delta * 5.0f);
			_camera.Transform = new Transform3D(_camera.Transform.Basis, smoothReturn);
		}
 
		// FOV Changes: 
		if (direction != Vector3.Zero)
		{
			if (isSprinting)
			{				
				float velocity_clamped = Mathf.Clamp(Velocity.Length(), 0.5f, Speed * SprintMultiplier * 2 );
				float target_fov = BASE_FOV + SPRINT_FOV * velocity_clamped; 
				_camera.Fov = Mathf.Lerp(_camera.Fov, target_fov, (float) delta*8.0f);
				if (_walkingSound.Playing) _walkingSound.Stop(); // Kill walk audio
				if (!_runningSound.Playing) _runningSound.Play(); // Start run audio
			}
			else
			{ 
				if(!_isZoomed)
				{
					_camera.Fov = Mathf.Lerp(_camera.Fov, BASE_FOV, (float) delta*8.0f);
				}
				
				if (_runningSound.Playing) _runningSound.Stop(); // Kill run audio
				if (!_walkingSound.Playing) _walkingSound.Play(); // Start walk audio
			}
		}
		else
		{
			if(!_isZoomed)
			{
				_camera.Fov = Mathf.Lerp(_camera.Fov, BASE_FOV, (float) delta*8.0f);
			}
			
			// Stop all sounds if airborne or motionless
			if (_walkingSound.Playing) _walkingSound.Stop();
			if (_runningSound.Playing) _runningSound.Stop();
		}


		MoveAndSlide(); // Required in any Godot Movement. 
		
		}


	/// <summary>
	/// Handles the headbob that we see while walking or sprinting. 
	/// </summary>
	private Vector3 _headbob(float Time)
	{
		float z = _camera.Position.Z;
		Vector3 pos = new Vector3(0f,0f,z); 
		pos.Y = Mathf.Sin(Time * _BOB_FREQ) *_BOB_AMP;
		pos.X = Mathf.Cos(Time *_BOB_FREQ / 2) * _BOB_AMP;
		return pos;
	}

	/// <summary>
	/// Handles dropping an item at current position (with a slight offset just in case). As of 8.23.26, there is probably a bug where we can drop items through a wall. 
	/// </summary>
	public void  drop_from_player(ItemData item)
	{
		Vector3 forward = -_camera.GlobalTransform.Basis.Z;
		forward = forward.Normalized();
		Vector3 drop_pos = GlobalPosition + forward * 4.0f; 
		if (item.InteractableScene != null)
		{
			// 2. Instantiate as Interactables (not just Node3D) so we can assign ItemData
			Interactables droppedItemNode = item.InteractableScene.Instantiate<Interactables>();
 
			// 3. Assign ItemData BEFORE adding to the tree, so _Ready() sees it
			// and actually spawns the mesh/collision (this was missing before,
			// which is why this correctly-positioned copy was invisible).
			droppedItemNode.ItemData = item;
 
			// 4. Add the new object to the main scene tree. 
			// We add it to the CurrentScene so it isn't attached to the moving player!
			GetTree().CurrentScene.AddChild(droppedItemNode);
			
			// 5. Set the GlobalPosition on the newly spawned node.
			droppedItemNode.GlobalPosition = drop_pos;
			GD.Print(drop_pos);
		}
	}

}
