using Godot;

/// <summary>The main base, resource treasury, and battle defeat target.</summary>
public partial class Headquarters : Building
{
    [Signal]
    public delegate void ResourcesChangedEventHandler(int silver, int armyPay);

    public int Silver { get; private set; }
    public int ArmyPay { get; private set; }

    private BuildGrid _grid;
    private Vector2I _reservedCell;
    private bool _hasReservedCell;

    public override void _Ready()
    {
        if (Data is not HeadquartersData config)
        {
            GD.PushError($"{Name} needs a HeadquartersData resource.");
            return;
        }

        base._Ready();
        Silver = Mathf.Max(0, config.StartingSilver);
        ArmyPay = Mathf.Max(0, config.StartingArmyPay);

        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged += OnGameStateChanged;

        _grid = GetNodeOrNull<BuildGrid>("../../BuildGrid");
        if (_grid != null)
        {
            _reservedCell = _grid.WorldToCell(GlobalPosition);
            _hasReservedCell = _grid.TryOccupy(_reservedCell, Vector2I.One);
            if (!_hasReservedCell)
                GD.PushWarning($"{Name} could not reserve grid cell {_reservedCell}.");
        }
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged -= OnGameStateChanged;
        if (_hasReservedCell && GodotObject.IsInstanceValid(_grid))
            _grid.Release(_reservedCell, Vector2I.One);
    }

    public bool TrySpendSilver(int amount)
    {
        if (amount < 0 || amount > Silver)
            return false;
        Silver -= amount;
        EmitResourcesChanged();
        return true;
    }

    public bool TrySpendArmyPay(int amount)
    {
        if (amount < 0 || amount > ArmyPay)
            return false;
        ArmyPay -= amount;
        EmitResourcesChanged();
        return true;
    }

    public void AddSilver(int amount)
    {
        if (amount <= 0)
            return;
        Silver = (int)Mathf.Min(int.MaxValue, (long)Silver + amount);
        EmitResourcesChanged();
    }

    public void AddArmyPay(int amount)
    {
        if (amount <= 0)
            return;
        ArmyPay = (int)Mathf.Min(int.MaxValue, (long)ArmyPay + amount);
        EmitResourcesChanged();
    }

    protected override void OnDestroyed()
    {
        // Keep the base visible so a defeat screen can inspect it after combat stops.
        if (GameManager.Instance?.CurrentState == GameState.FightLayer)
            GameManager.Instance.FailBattle();
    }

    private void OnGameStateChanged(GameState previousState, GameState currentState)
    {
        if (currentState == GameState.FightLayer && Health <= 0)
            GameManager.Instance.FailBattle();
    }

    private void EmitResourcesChanged() =>
        EmitSignal(SignalName.ResourcesChanged, Silver, ArmyPay);
}
