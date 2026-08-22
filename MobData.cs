using Godot;

[GlobalClass]
public partial class MobData : Resource
{
    [Export] public string DisplayName { get; set; }
    [Export] public int MaxHealth { get; set; } = 10;
    [Export] public float MinSpeed { get; set; } = 1.0f;
    [Export] public float MaxSpeed { get; set; } = 60.0f; 
    [Export] public bool isReal { get; set; } = false; // False for Hallucinations. 
    [Export] public int AttackDamage { get; set; } = 5;
    [Export] public PackedScene MeshScene { get; set; }
}