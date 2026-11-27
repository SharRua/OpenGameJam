using Godot;

// Starting resources are configuration; current balances live on Headquarters.
[GlobalClass]
public partial class HeadquartersData : BuildingData
{
    [Export] public int StartingSilver { get; set; } = 500;
    [Export] public int StartingArmyPay { get; set; } = 100;
}
