using Godot;

public partial class Building : Node2D
{
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

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
        if (damage <= 0 || Health <= 0)
            return;

        Health = Mathf.Max(0, Health - damage);
        EmitSignal(SignalName.HealthChanged, Health, MaxHealth);
        if (Health == 0)
            OnDestroyed();
    }

    protected virtual void OnDestroyed() => QueueFree();
}
