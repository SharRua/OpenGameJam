using Godot;

[GlobalClass]
public partial class TowerData : BuildingData
{
    [Export] public float Damage { get; set; } = 10f;
    [Export] public float AttackInterval { get; set; } = 1f;
    [Export] public float AttackRangeRadius { get; set; } = 150f;
    [Export] public float AttackAreaRadius { get; set; } = 50f;
}
