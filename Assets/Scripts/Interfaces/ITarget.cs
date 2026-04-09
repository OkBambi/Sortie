using UnityEngine;

public interface ITarget
{
    Transform Transform { get; }
    Vector3 Velocity { get; }
    bool IsValid { get; }
}
