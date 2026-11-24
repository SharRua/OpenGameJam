using Godot;
using System;

[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string EnemyName { get; set; }
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float MoveSpeed { get; set; } = 100f;
    [Export] public float AttackDamage { get; set; } = 10f;
    [Export] public float AttackInterval { get; set; } = 1f;
    [Export] public float AttackRange { get; set; } = 150f;
}
