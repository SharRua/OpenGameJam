using System.Collections.Generic;
using Godot;

/// <summary>Queries the soldier deployment slots authored on the rampart tiles.</summary>
public partial class YongchangFortressMap : Node2D
{
    [Export] public TileMapLayer MapTiles { get; set; }

    private readonly HashSet<Vector2I> _occupiedSoldierCells = new();

    public override void _Ready()
    {
        MapTiles ??= GetNodeOrNull<TileMapLayer>("MapTiles");
    }

    public Vector2I WorldToCell(Vector2 worldPosition) =>
        MapTiles.LocalToMap(MapTiles.ToLocal(worldPosition));

    public Vector2 CellToWorldCenter(Vector2I cell) =>
        MapTiles.ToGlobal(MapTiles.MapToLocal(cell));

    public bool CanDeploySoldier(Vector2 worldPosition) =>
        CanDeploySoldier(WorldToCell(worldPosition));

    public bool CanDeploySoldier(Vector2I cell)
    {
        TileData tile = MapTiles?.GetCellTileData(cell);
        return tile != null && tile.GetCustomData("CanDeploySoldier").AsBool();
    }

    /// <summary>Places a Node2D soldier on an unoccupied rampart slot.</summary>
    public Node2D TryDeploySoldier(PackedScene soldierScene, Vector2 worldPosition)
    {
        if (soldierScene == null || MapTiles == null)
            return null;

        Vector2I cell = WorldToCell(worldPosition);
        if (!CanDeploySoldier(cell) || _occupiedSoldierCells.Contains(cell))
            return null;

        Node2D soldier = soldierScene.Instantiate<Node2D>();
        Node2D soldiersRoot = GetNodeOrNull<Node2D>("Soldiers") ?? this;
        soldiersRoot.AddChild(soldier);
        soldier.GlobalPosition = CellToWorldCenter(cell);
        _occupiedSoldierCells.Add(cell);
        soldier.TreeExited += () => _occupiedSoldierCells.Remove(cell);
        return soldier;
    }
}
