using Godot;

public partial class CameraMoveFollow : CharacterBody2D
{
    [Export]
    public float Speed { get; set; } = 400f;

    [Export]
    public CollisionShape2D MovementBounds { get; set; }

    private Camera2D _camera;

    public override void _Ready()
    {
        _camera = GetNodeOrNull<Camera2D>("../MainCamera");
        MovementBounds ??= GetNodeOrNull<CollisionShape2D>("../CameraBounds/CollisionShape2D");
        if (_camera == null)
            GD.PushError("CameraMoveFollow needs a sibling Camera2D named MainCamera.");
        if (MovementBounds?.Shape is not RectangleShape2D)
            GD.PushError("CameraMoveFollow needs a RectangleShape2D in MovementBounds.");
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = Input.GetVector(
            "move_left", "move_right", "move_up", "move_down") * Speed;

        MoveAndSlide();
        KeepFollowPointInsideCameraTravel();
    }

    public override void _Process(double delta)
    {
        // The camera interpolates zoom each rendered frame, so update the limit here too.
        KeepFollowPointInsideCameraTravel();
    }

    private void KeepFollowPointInsideCameraTravel()
    {
        if (_camera == null || MovementBounds?.Shape is not RectangleShape2D rectangle)
            return;

        Vector2 zoom = _camera.Zoom;
        if (zoom.X <= 0f || zoom.Y <= 0f)
            return;

        Vector2 halfView = GetViewport().GetVisibleRect().Size / (2f * zoom);

        Vector2 halfBounds = rectangle.Size / 2f;
        Vector2 worldMin = MovementBounds.ToGlobal(-halfBounds);
        Vector2 worldMax = MovementBounds.ToGlobal(halfBounds);

        // Use the intersection of the shape and the camera's actual limits.
        // Phantom Camera can otherwise stop the view before this target stops.
        float boundsLeft = Mathf.Max(worldMin.X, _camera.LimitLeft);
        float boundsRight = Mathf.Min(worldMax.X, _camera.LimitRight);
        float boundsTop = Mathf.Max(worldMin.Y, _camera.LimitTop);
        float boundsBottom = Mathf.Min(worldMax.Y, _camera.LimitBottom);

        float minX = boundsLeft + halfView.X;
        float maxX = boundsRight - halfView.X;
        float minY = boundsTop + halfView.Y;
        float maxY = boundsBottom - halfView.Y;

        // If the view is larger than the allowed area, keep the target centered.
        float x = minX <= maxX
            ? Mathf.Clamp(GlobalPosition.X, minX, maxX)
            : (boundsLeft + boundsRight) / 2f;
        float y = minY <= maxY
            ? Mathf.Clamp(GlobalPosition.Y, minY, maxY)
            : (boundsTop + boundsBottom) / 2f;

        GlobalPosition = new Vector2(x, y);
    }
}
