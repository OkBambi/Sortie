using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour, IDamage
{
    [Header("Universal Stats")]
    public int maxHealth;
    protected int currentHealth;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
    }

    protected virtual void Update()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public abstract void TakeDamage(float damage);
}
