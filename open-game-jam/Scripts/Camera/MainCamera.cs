using Godot;
using PhantomCamera;

public partial class MainCamera : Camera2D
{
    [Export(PropertyHint.Range, "1.01,3,0.01")]
    public float ZoomStep { get; set; } = 1.08f;

    [Export(PropertyHint.Range, "0.05,1,0.05")]
    public float MinZoom { get; set; } = 0.25f;

    [Export(PropertyHint.Range, "0.05,10,0.05")]
    public float MaxZoom { get; set; } = 0.75f;

    [Export(PropertyHint.Range, "1,30,0.5")]
    public float ZoomSmoothness { get; set; } = 10f;

    [Export]
    public NodePath BoundsShapePath { get; set; } = new("../CameraBounds/CollisionShape2D");
    

    private bool _isPanning;
    private CollisionShape2D _boundsShape;
    
    private PhantomCamera2D _pcam;
    private float _targetZoom;

    public override void _Ready()
    {
        _pcam = GetNode<Node2D>("../PhantomCamera2D").AsPhantomCamera2D();
        _targetZoom = _pcam.Zoom.X;
        _boundsShape = GetNodeOrNull<CollisionShape2D>(BoundsShapePath);
        if (_boundsShape?.Shape is not RectangleShape2D)
            GD.PushWarning("MainCamera needs a RectangleShape2D at BoundsShapePath.");

        KeepViewInsideBounds();
    }

    public override void _Process(double delta)
    {
        float minimumZoom = GetMinimumZoomToFitBounds();
        _targetZoom = Mathf.Clamp(_targetZoom, minimumZoom, Mathf.Max(MaxZoom, minimumZoom));

        float blend = 1f - Mathf.Exp(-ZoomSmoothness * (float)delta);
        float nextZoom = Mathf.Lerp(_pcam.Zoom.X, _targetZoom, blend);
        if (Mathf.Abs(nextZoom - _targetZoom) < 0.001f)
            nextZoom = _targetZoom;

        _pcam.Zoom = Vector2.One * nextZoom;
        Zoom = _pcam.Zoom;
        KeepViewInsideBounds();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            if (button.Pressed && button.ButtonIndex == MouseButton.WheelUp)
            {
                _targetZoom *= ZoomStep;
                GetViewport().SetInputAsHandled();
            }
            else if (button.Pressed && button.ButtonIndex == MouseButton.WheelDown)
            {
                _targetZoom /= ZoomStep;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void ZoomAtMouse(float factor, Vector2 mousePosition)
    {
        float newZoom = Mathf.Clamp(Zoom.X * factor, MinZoom, MaxZoom);
        if (Mathf.IsEqualApprox(newZoom, Zoom.X))
            return;

        Viewport viewport = GetViewport();
        Vector2 mouseWorldBefore = viewport.GetCanvasTransform().AffineInverse() * mousePosition;
        Zoom = Vector2.One * newZoom;
        ForceUpdateScroll();
        Vector2 mouseWorldAfter = viewport.GetCanvasTransform().AffineInverse() * mousePosition;
        GlobalPosition += mouseWorldBefore - mouseWorldAfter;
        ForceUpdateScroll();
        KeepViewInsideBounds();
    }

    private float GetMinimumZoomToFitBounds()
    {
        if (_boundsShape?.Shape is not RectangleShape2D rectangle)
            return MinZoom;

        Vector2 halfSize = rectangle.Size / 2f;
        Vector2 worldSize = _boundsShape.ToGlobal(halfSize) - _boundsShape.ToGlobal(-halfSize);
        Vector2 screenSize = GetViewport().GetVisibleRect().Size;
        float fitZoom = Mathf.Max(
            screenSize.X / Mathf.Abs(worldSize.X),
            screenSize.Y / Mathf.Abs(worldSize.Y));
        return Mathf.Max(MinZoom, fitZoom);
    }

    private void KeepViewInsideBounds()
    {
        if (_boundsShape?.Shape is not RectangleShape2D rectangle)
            return;

        Rect2 view = GetViewRectInBounds();
        float fitFactor = Mathf.Max(view.Size.X / rectangle.Size.X, view.Size.Y / rectangle.Size.Y);
        if (fitFactor > 1.0f)
        {
            // Keep the entire view inside after zooming out or resizing.
            Zoom = Vector2.One * (Zoom.X * fitFactor);
            ForceUpdateScroll();
            view = GetViewRectInBounds();
        }

        Vector2 halfSize = rectangle.Size / 2.0f;
        Vector2 shift = new(
            GetAxisShift(view.Position.X, view.End.X, -halfSize.X, halfSize.X),
            GetAxisShift(view.Position.Y, view.End.Y, -halfSize.Y, halfSize.Y));

        if (!shift.IsZeroApprox())
        {
            GlobalPosition += _boundsShape.ToGlobal(shift) - _boundsShape.GlobalPosition;
            ForceUpdateScroll();
        }
    }

    private Rect2 GetViewRectInBounds()
    {
        Rect2 screen = GetViewport().GetVisibleRect();
        Transform2D screenToWorld = GetViewport().GetCanvasTransform().AffineInverse();
        Vector2 topLeft = _boundsShape.ToLocal(screenToWorld * screen.Position);
        Vector2 topRight = _boundsShape.ToLocal(screenToWorld * (screen.Position + new Vector2(screen.Size.X, 0)));
        Vector2 bottomLeft = _boundsShape.ToLocal(screenToWorld * (screen.Position + new Vector2(0, screen.Size.Y)));
        Vector2 bottomRight = _boundsShape.ToLocal(screenToWorld * (screen.Position + screen.Size));

        float minX = Mathf.Min(Mathf.Min(topLeft.X, topRight.X), Mathf.Min(bottomLeft.X, bottomRight.X));
        float minY = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y), Mathf.Min(bottomLeft.Y, bottomRight.Y));
        float maxX = Mathf.Max(Mathf.Max(topLeft.X, topRight.X), Mathf.Max(bottomLeft.X, bottomRight.X));
        float maxY = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y), Mathf.Max(bottomLeft.Y, bottomRight.Y));
        return new Rect2(new Vector2(minX, minY), new Vector2(maxX - minX, maxY - minY));
    }

    private static float GetAxisShift(float viewMin, float viewMax, float boundsMin, float boundsMax)
    {
        if (viewMax - viewMin > boundsMax - boundsMin)
            return (boundsMin + boundsMax - viewMin - viewMax) / 2.0f;
        if (viewMin < boundsMin)
            return boundsMin - viewMin;
        if (viewMax > boundsMax)
            return boundsMax - viewMax;
        return 0.0f;
    }
}
