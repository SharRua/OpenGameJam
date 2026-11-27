using Godot;

/// <summary>Moves the camera target in both phases and shows the general only during LiveLayer.</summary>
public partial class PlayerController : CameraMoveFollow
{
    private static readonly StringName IdleAnimation = "Idle";
    private static readonly StringName WalkAnimation = "Walk";

    [Export] public AnimatedSprite2D CharacterSprite { get; set; }

    public override void _Ready()
    {
        base._Ready();
        CharacterSprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        if (CharacterSprite == null)
        {
            GD.PushError($"{Name} needs an AnimatedSprite2D child.");
            return;
        }

        if (GameManager.Instance == null)
        {
            GD.PushError($"{Name} needs the GameManager autoload.");
            CharacterSprite.Hide();
            return;
        }

        GameManager.Instance.StateChanged += OnStateChanged;
        ApplyState(GameManager.Instance.CurrentState);
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged -= OnStateChanged;
    }

    public override void _PhysicsProcess(double delta)
    {
        GameState state = GameManager.Instance?.CurrentState ?? GameState.Start;
        if (state != GameState.LiveLayer && state != GameState.FightLayer)
        {
            Velocity = Vector2.Zero;
            return;
        }

        base._PhysicsProcess(delta);
        if (state == GameState.LiveLayer)
            UpdateAnimation();
    }

    private void OnStateChanged(GameState previousState, GameState currentState) =>
        ApplyState(currentState);

    private void ApplyState(GameState state)
    {
        if (CharacterSprite == null)
            return;

        if (state == GameState.LiveLayer)
        {
            CharacterSprite.Show();
            UpdateAnimation();
        }
        else if (state == GameState.Pause)
        {
            CharacterSprite.Visible = GameManager.Instance?.StateBeforePause == GameState.LiveLayer;
        }
        else
        {
            CharacterSprite.Hide();
            CharacterSprite.Stop();
        }
    }

    private void UpdateAnimation()
    {
        if (Velocity.X < -0.01f)
            CharacterSprite.FlipH = true;
        else if (Velocity.X > 0.01f)
            CharacterSprite.FlipH = false;

        StringName animation = Velocity.LengthSquared() > 0.01f
            ? WalkAnimation
            : IdleAnimation;

        if (CharacterSprite.Animation != animation || !CharacterSprite.IsPlaying())
            CharacterSprite.Play(animation);
    }
}
