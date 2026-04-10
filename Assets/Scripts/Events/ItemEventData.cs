using UnityEngine;

public class ItemEventData
{
    public MonoBehaviour Owner;
    public ITarget Target;
}

public class DamageEventData : ItemEventData
{
    public float DamageAmount;
    public bool IsCriticalHit;
}

public class HealEventData : ItemEventData
{
    public float HealAmount;
}

public class PositionEventData : ItemEventData
{
    public Vector3 Position;
}