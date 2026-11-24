using System.Collections.Generic;
using Godot;

public partial class BuildManager : Node
{
    [Export] public BuildGrid Grid { get; set; }
    [Export] public Node2D BuildingsRoot { get; set; }
    [Export] public PackedScene ArcherScene { get; set; }

    private static readonly Vector2I ArcherFootprint = new(1, 1);
    private readonly Dictionary<Vector2I, Tower> _towersByCell = new();
    private Tower _editingTower;

    public override void _Ready()
    {
        Grid ??= GetNodeOrNull<BuildGrid>("../BuildGrid");
        BuildingsRoot ??= GetNodeOrNull<Node2D>("../Buildings");
        ArcherScene ??= GD.Load<PackedScene>("res://Scripts/Core/Archer.tscn");

        if (Grid == null || BuildingsRoot == null || ArcherScene == null)
            GD.PushError("BuildManager needs BuildGrid, Buildings, and Archer.tscn.");
    }

    public override void _Process(double delta)
    {
        if (GodotObject.IsInstanceValid(_editingTower))
            _editingTower.PreviewAttackArea(ToWorld(GetViewport().GetMousePosition()));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && _editingTower != null)
        {
            _editingTower.PreviewAttackArea(ToWorld(motion.Position));
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is not InputEventMouseButton button || !button.Pressed)
            return;

        if (Grid == null || BuildingsRoot == null || ArcherScene == null)
            return;

        if (_editingTower != null)
        {
            if (button.ButtonIndex == MouseButton.Right)
            {
                _editingTower.CancelAttackAreaSetting();
                GD.Print("Cancelled AttackArea setting.");
                _editingTower = null;
                GetViewport().SetInputAsHandled();
            }
            else if (button.ButtonIndex == MouseButton.Left)
            {
                _editingTower.PreviewAttackArea(ToWorld(button.Position));
                _editingTower.ApplyAttackAreaSetting();
                GD.Print($"Saved AttackArea position: {_editingTower.SavedAttackAreaPosition}");
                _editingTower = null;
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        if (button.ButtonIndex != MouseButton.Left &&
            button.ButtonIndex != MouseButton.Right)
            return;

        Vector2 worldPosition = ToWorld(button.Position);
        Vector2I cell = Grid.WorldToCell(worldPosition);

        if (_towersByCell.TryGetValue(cell, out Tower tower) &&
            GodotObject.IsInstanceValid(tower))
        {
            if (tower.BeginAttackAreaSetting())
            {
                _editingTower = tower;
                tower.PreviewAttackArea(worldPosition);
                GD.Print($"Setting AttackArea for tower in cell {cell}");
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (button.ButtonIndex == MouseButton.Right)
        {
            GetViewport().SetInputAsHandled();
            BuildArcher(cell);
        }
    }

    private Vector2 ToWorld(Vector2 viewportPosition) =>
        GetViewport().GetCanvasTransform().AffineInverse() * viewportPosition;

    private void BuildArcher(Vector2I cell)
    {
        GD.Print($"Right-clicked grid cell {cell}");

        if (!Grid.CanPlace(cell, ArcherFootprint))
        {
            GD.Print($"Cannot build Archer in cell {cell}: outside grid or occupied.");
            return;
        }

        Tower archer = ArcherScene.Instantiate<Tower>();
        if (!Grid.TryOccupy(cell, ArcherFootprint))
        {
            archer.QueueFree();
            return;
        }

        BuildingsRoot.AddChild(archer);
        archer.GlobalPosition = Grid.CellToWorldCenter(cell);
        _towersByCell[cell] = archer;

        archer.TreeExited += () =>
        {
            if (_editingTower == archer)
                _editingTower = null;
            _towersByCell.Remove(cell);
            if (GodotObject.IsInstanceValid(Grid))
                Grid.Release(cell, ArcherFootprint);
        };

        archer.OnPlaced();
        GD.Print($"Built Archer in cell {cell} at {archer.GlobalPosition}");
    }
}
