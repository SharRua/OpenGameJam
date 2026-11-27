using Godot;

/// <summary>Owns the game's phase and restores the previous phase after a pause.</summary>
public partial class GameManager : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(GameState previousState, GameState currentState);

    [Signal]
    public delegate void BattleFailedEventHandler();

    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Start;
    public GameState StateBeforePause { get; private set; } = GameState.Start;
    public bool BattleHasFailed { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool ChangeState(GameState nextState)
    {
        if (nextState == GameState.FightLayer && BattleHasFailed)
            return false;

        if (CurrentState == nextState)
            return false;

        if (nextState == GameState.Start || nextState == GameState.LiveLayer)
            BattleHasFailed = false;

        GameState previousState = CurrentState;
        if (nextState == GameState.Pause)
            StateBeforePause = previousState;

        CurrentState = nextState;
        GetTree().Paused = nextState == GameState.Pause;
        EmitSignal(SignalName.StateChanged, (int)previousState, (int)nextState);
        return true;
    }

    public bool EnterLiveLayer() => ChangeState(GameState.LiveLayer);
    public bool EnterFightLayer() => ChangeState(GameState.FightLayer);
    public bool ReturnToStart() => ChangeState(GameState.Start);
    public bool PauseGame() => ChangeState(GameState.Pause);

    public bool ResumeGame()
    {
        if (CurrentState != GameState.Pause || BattleHasFailed)
            return false;

        return ChangeState(StateBeforePause);
    }

    public bool FailBattle()
    {
        if (CurrentState != GameState.FightLayer || BattleHasFailed)
            return false;

        BattleHasFailed = true;
        PauseGame();
        EmitSignal(SignalName.BattleFailed);
        return true;
    }
}
