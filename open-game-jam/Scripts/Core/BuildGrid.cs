using System.Collections.Generic;
using Godot;

[Tool]
[GlobalClass]
public partial class BuildGrid : Node2D
{
    private readonly HashSet<Vector2I> _occupied = new();
    private int _cellSize = 128;
    private int _columns = 59;
    private int _rows = 33;
    private bool _showGridInGame;
    private Color _gridColor = new(0.2f, 0.8f, 1f, 0.3f);

    [Export(PropertyHint.Range, "1,512,1")]
    public int CellSize
    {
        get => _cellSize;
        set { _cellSize = Mathf.Max(1, value); RedrawIfReady(); }
    }

    [Export(PropertyHint.Range, "1,512,1")]
    public int Columns
    {
        get => _columns;
        set { _columns = Mathf.Max(1, value); RedrawIfReady(); }
    }

    [Export(PropertyHint.Range, "1,512,1")]
    public int Rows
    {
        get => _rows;
        set { _rows = Mathf.Max(1, value); RedrawIfReady(); }
    }

    [Export]
    public bool ShowGridInGame
    {
        get => _showGridInGame;
        set { _showGridInGame = value; RedrawIfReady(); }
    }

    [Export]
    public Color GridColor
    {
        get => _gridColor;
        set { _gridColor = value; RedrawIfReady(); }
    }

    public Vector2 GridLocalSize => new(_columns * _cellSize, _rows * _cellSize);
    private Vector2 LocalTopLeft => -GridLocalSize / 2f;

    public Vector2I WorldToCell(Vector2 worldPosition)
    {
        Vector2 local = ToLocal(worldPosition) - LocalTopLeft;
        return new Vector2I(
            Mathf.FloorToInt(local.X / _cellSize),
            Mathf.FloorToInt(local.Y / _cellSize));
    }

    public Vector2 CellToWorldCenter(Vector2I cell)
    {
        Vector2 local = LocalTopLeft + new Vector2(
            (cell.X + 0.5f) * _cellSize,
            (cell.Y + 0.5f) * _cellSize);
        return ToGlobal(local);
    }

    public bool IsInside(Vector2I cell) =>
        cell.X >= 0 && cell.X < _columns &&
        cell.Y >= 0 && cell.Y < _rows;

    public bool IsOccupied(Vector2I cell) => _occupied.Contains(cell);

    public bool CanPlace(Vector2I origin, Vector2I footprint)
    {
        if (footprint.X < 1 || footprint.Y < 1)
            return false;

        for (int y = 0; y < footprint.Y; y++)
        for (int x = 0; x < footprint.X; x++)
        {
            Vector2I cell = origin + new Vector2I(x, y);
            if (!IsInside(cell) || IsOccupied(cell))
                return false;
        }

        return true;
    }

    public bool TryOccupy(Vector2I origin, Vector2I footprint)
    {
        if (!CanPlace(origin, footprint))
            return false;

        for (int y = 0; y < footprint.Y; y++)
        for (int x = 0; x < footprint.X; x++)
            _occupied.Add(origin + new Vector2I(x, y));

        return true;
    }

    public void Release(Vector2I origin, Vector2I footprint)
    {
        for (int y = 0; y < footprint.Y; y++)
        for (int x = 0; x < footprint.X; x++)
            _occupied.Remove(origin + new Vector2I(x, y));
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint() && !_showGridInGame)
            return;

        Vector2 topLeft = LocalTopLeft;
        Vector2 size = GridLocalSize;

        for (int x = 0; x <= _columns; x++)
        {
            float localX = topLeft.X + x * _cellSize;
            DrawLine(new Vector2(localX, topLeft.Y),
                new Vector2(localX, topLeft.Y + size.Y), _gridColor);
        }

        for (int y = 0; y <= _rows; y++)
        {
            float localY = topLeft.Y + y * _cellSize;
            DrawLine(new Vector2(topLeft.X, localY),
                new Vector2(topLeft.X + size.X, localY), _gridColor);
        }
    }

    private void RedrawIfReady()
    {
        if (IsInsideTree())
            QueueRedraw();
    }
}
