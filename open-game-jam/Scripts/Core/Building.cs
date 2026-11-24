using Godot;
using System;

public partial class Building : Node2D
{
    [Export] public int MaxHealth { get; set; } = 100;
    public int Health { get; private set; }

    public override void _Ready() => Health = MaxHealth;

    public virtual void OnPlaced() { }

    public void TakeDamage(int damage)
    {
        Health = Mathf.Max(0, Health - damage);
        if (Health == 0) QueueFree();
    }
}