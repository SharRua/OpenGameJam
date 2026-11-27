using System.Collections.Generic;
using Godot;

public partial class BuildManager : Node
{
    [Export] public BuildGrid Grid { get; set; }
    [Export] public Node2D BuildingsRoot { get; set; }
    [Export] public Headquarters Headquarters { get; set; }
    [Export] public PackedScene ArcherScene { get; set; }

    private static readonly Vector2I ArcherFootprint = new(1, 1);
    private readonly Dictionary<Vector2I, Tower> _towersByCell = new();
    private Tower _editingTower;

    public override void _Ready()
    {
        Grid ??= GetNodeOrNull<BuildGrid>("../BuildGrid");
        BuildingsRoot ??= GetNodeOrNull<Node2D>("../Buildings");
        Headquarters ??= GetNodeOrNull<Headquarters>("../Buildings/Headquarters");
        ArcherScene ??= GD.Load<PackedScene>("res://Scripts/Core/Archer.tscn");

        if (Grid == null || BuildingsRoot == null || Headquarters == null || ArcherScene == null)
            GD.PushError("BuildManager needs BuildGrid, Buildings, Headquarters, and Archer.tscn.");
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

        if (Grid == null || BuildingsRoot == null || Headquarters == null || ArcherScene == null)
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
            TryBuildTower(ArcherScene, cell);
        }
    }

    private Vector2 ToWorld(Vector2 viewportPosition) =>
        GetViewport().GetCanvasTransform().AffineInverse() * viewportPosition;

    public bool TryBuildTower(PackedScene towerScene, Vector2I cell)
    {
        if (Grid == null || BuildingsRoot == null ||
            !GodotObject.IsInstanceValid(Headquarters) || towerScene == null)
            return false;

        if (!Grid.CanPlace(cell, ArcherFootprint))
        {
            GD.Print($"Cannot build tower in cell {cell}: outside grid or occupied.");
            return false;
        }

        Node instance = towerScene.Instantiate();
        if (instance is not Tower tower)
        {
            instance?.Free();
            GD.PushError("The selected tower scene must have a Tower root node.");
            return false;
        }

        if (tower.TowerStats is not TowerData stats || stats.SilverCost < 0)
        {
            tower.Free();
            GD.PushError("The selected tower needs TowerData with a nonnegative SilverCost.");
            return false;
        }

        if (!Grid.TryOccupy(cell, ArcherFootprint))
        {
            tower.Free();
            return false;
        }

        if (!Headquarters.TrySpendSilver(stats.SilverCost))
        {
            Grid.Release(cell, ArcherFootprint);
            tower.Free();
            GD.Print($"Cannot build {stats.DisplayName}: need {stats.SilverCost} silver, have {Headquarters.Silver}.");
            return false;
        }

        BuildingsRoot.AddChild(tower);
        tower.GlobalPosition = Grid.CellToWorldCenter(cell);
        _towersByCell[cell] = tower;

        tower.TreeExited += () =>
        {
            if (_editingTower == tower)
                _editingTower = null;
            _towersByCell.Remove(cell);
            if (GodotObject.IsInstanceValid(Grid))
                Grid.Release(cell, ArcherFootprint);
        };

        tower.OnPlaced();
        GD.Print($"Built {stats.DisplayName} in cell {cell} at {tower.GlobalPosition} for {stats.SilverCost} silver.");
        return true;
    }
}
