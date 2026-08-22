using Godot;

// Standalone helper, same shape as Itemsfx — hand it any Node3D that's
// currently in the tree and it hangs a temporary SpotLight3D off it.
// Parenting the light to the actual item mesh (rather than the view camera)
// means it automatically tracks that mesh's position/rotation every frame —
// no manual per-frame syncing needed.
public static class CameraFlashFx
{
    public static void Trigger(Node3D attachTo, float peakEnergy = 50.0f, float range = 20.0f, float spotAngleDegrees = 20.0f, float duration = 5.0f)
    {
        if (attachTo == null)
        {
            GD.Print("FUCK ITS NULL");
            return;
        } 

        SpotLight3D flash = new SpotLight3D();
        flash.LightEnergy = peakEnergy;
        flash.LightColor = Colors.White;
        flash.SpotRange = range;
        flash.SpotAngle = spotAngleDegrees;        // half-angle of the cone, in degrees
        flash.SpotAngleAttenuation = 3.0f;         // how sharply the light falls off toward the cone's edge
        flash.ShadowEnabled = false;               // shadow-casting lights are costly — flip on if you want real shadows and can afford it

        attachTo.AddChild(flash);
        flash.Position = Vector3.Zero;
        flash.Rotation = Vector3.Zero; // assumes attachTo's own -Z already faces "forward" — see note below

        // Discharge curve: fast drop at first, tapering toward zero — same
        // shape as a capacitor discharging (V(t) = V0 * e^-t/RC).
        // Expo/Out gives you that exact shape in Godot's Tween.
        Tween flashTween = attachTo.CreateTween();
        flashTween.SetTrans(Tween.TransitionType.Expo);
        flashTween.SetEase(Tween.EaseType.Out);
        flashTween.TweenProperty(flash, "light_energy", 0.0f, duration);
        flashTween.TweenCallback(Callable.From(flash.QueueFree));
    }
}