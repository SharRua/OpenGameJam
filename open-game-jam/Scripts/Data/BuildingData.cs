using Godot;

// Template values for a building type. Runtime state belongs on Building.
[GlobalClass]
public partial class BuildingData : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int BuildCost { get; set; } = 50;
    [Export] public int StartingLevel { get; set; } = 1;
    [Export] public Godot.Collections.Array<BuildingSkillData> Skills { get; set; } = new();
}
