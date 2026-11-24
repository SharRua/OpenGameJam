using Godot;

public partial class Tower : Building
{
    [Export] public float BaseDamage { get; set; }
    [Export] public float AttackInterval { get; set; }
    [Export] public float AttackRangeRadius { get; set; }
    [Export] public float AttackAreaRadius { get; set; }
    [Export] public CollisionShape2D AttackRange { get; set; }
    [Export] public CollisionShape2D AttackArea { get; set; }

    private bool _isSettingAttackArea;
    private Vector2 _positionBeforeSetting;

    public Vector2 SavedAttackAreaPosition { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        AttackRange ??= GetNodeOrNull<CollisionShape2D>("Range/Attack range");
        AttackArea ??= GetNodeOrNull<CollisionShape2D>("Range/Attack Area");
        UpdateAttackRange();

        if (AttackArea != null)
            SavedAttackAreaPosition = AttackArea.Position;
    }

    private void UpdateAttackRange()
    {
        if (AttackRange == null || AttackArea == null)
        {
            GD.PushWarning($"{Name} needs both attack collision shapes.");
            return;
        }

        if (AttackRange.Shape is not CircleShape2D rangeCircle ||
            AttackArea.Shape is not CircleShape2D areaCircle)
        {
            GD.PushWarning($"{Name} needs two CircleShape2D resources.");
            return;
        }

        // Give each tower its own shape resource so later upgrades do not alter other towers.
        var rangeCopy = (CircleShape2D)rangeCircle.Duplicate();
        rangeCopy.Radius = Mathf.Max(1f, AttackRangeRadius);
        AttackRange.Shape = rangeCopy;
        AttackRange.Scale = Vector2.One;

        var areaCopy = (CircleShape2D)areaCircle.Duplicate();
        areaCopy.Radius = Mathf.Max(1f, AttackAreaRadius);
        AttackArea.Shape = areaCopy;
        AttackArea.Scale = Vector2.One;
    }

    public bool BeginAttackAreaSetting()
    {
        if (AttackRange?.Shape is not CircleShape2D ||
            AttackArea?.Shape is not CircleShape2D)
            return false;

        _positionBeforeSetting = AttackArea.Position;
        _isSettingAttackArea = true;
        QueueRedraw();
        return true;
    }

    public void PreviewAttackArea(Vector2 mouseWorldPosition)
    {
        if (!_isSettingAttackArea || AttackRange?.Shape is not CircleShape2D circle)
            return;

        Node2D rangeNode = AttackRange.GetParent() as Node2D;
        if (rangeNode == null)
            return;

        Vector2 mouseInRange = rangeNode.ToLocal(mouseWorldPosition);
        Vector2 offset = mouseInRange - AttackRange.Position;
        float maxDistance = circle.Radius * Mathf.Abs(AttackRange.Scale.X);
        AttackArea.Position = AttackRange.Position + offset.LimitLength(maxDistance);
        QueueRedraw();
    }

    public void ApplyAttackAreaSetting()
    {
        if (!_isSettingAttackArea)
            return;

        SavedAttackAreaPosition = AttackArea.Position;
        _isSettingAttackArea = false;
        QueueRedraw();
    }

    public void CancelAttackAreaSetting()
    {
        if (!_isSettingAttackArea)
            return;

        AttackArea.Position = _positionBeforeSetting;
        _isSettingAttackArea = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_isSettingAttackArea ||
            AttackRange?.Shape is not CircleShape2D ||
            AttackArea?.Shape is not CircleShape2D)
            return;

        Vector2 rangeCenter = ToLocal(AttackRange.GlobalPosition);
        float rangeRadius = GetDrawRadius(AttackRange);
        Vector2 areaCenter = ToLocal(AttackArea.GlobalPosition);
        float areaRadius = GetDrawRadius(AttackArea);

        DrawArc(rangeCenter, rangeRadius, 0f, Mathf.Tau, 96,
            new Color(0.2f, 0.9f, 0.3f, 0.95f), 3f, true);
        DrawCircle(areaCenter, areaRadius,
            new Color(1f, 0.6f, 0.1f, 0.18f));
        DrawArc(areaCenter, areaRadius, 0f, Mathf.Tau, 64,
            new Color(1f, 0.6f, 0.1f, 0.95f), 2f, true);
    }

    private float GetDrawRadius(CollisionShape2D collisionShape)
    {
        var circle = (CircleShape2D)collisionShape.Shape;
        Vector2 center = ToLocal(collisionShape.GlobalPosition);
        Vector2 edge = ToLocal(collisionShape.ToGlobal(new Vector2(circle.Radius, 0f)));
        return center.DistanceTo(edge);
    }
}
