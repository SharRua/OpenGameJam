using Godot;

public partial class Building : Node2D
{
    [Export] public BuildingData Data { get; set; }

    public int MaxHealth => Data?.MaxHealth ?? 0;
    public int BuildCost => Data?.BuildCost ?? 0;
    public int Health { get; private set; }
    public int Level { get; private set; }

    public override void _Ready()
    {
        if (Data == null)
        {
            GD.PushError($"{Name} needs a BuildingData resource.");
            return;
        }

        Health = Mathf.Max(1, Data.MaxHealth);
        Level = Mathf.Max(1, Data.StartingLevel);
    }

    public virtual void OnPlaced() { }

    public void TakeDamage(int damage)
    {
        Health = Mathf.Max(0, Health - damage);
        if (Health == 0) QueueFree();
    }
}
