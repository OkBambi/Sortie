public interface IHealth
{
    float CurrentHealth { get; }
    float MaxHealth { get; }

    void ChangeHealth(float amount);
}