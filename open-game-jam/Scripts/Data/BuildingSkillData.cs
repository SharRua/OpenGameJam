using Godot;

// Configuration shared by all instances using the same skill resource.
[GlobalClass]
public partial class BuildingSkillData : Resource
{
    [Export] public string SkillId { get; set; } = "";
    [Export] public float Cooldown { get; set; } = 0f;
}
