using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour, IDamage, ITarget, IHealth
{
    [Header("Universal Stats")]
    public float maxHealth;
    protected float currentHealth;

    private Vector3 _lastPosition;
    private Vector3 _currentVelocity;

    public Transform Transform => transform;

    public Vector3 Velocity => _currentVelocity;

    public bool IsValid => currentHealth > 0 && gameObject.activeInHierarchy;

    public float CurrentHealth => currentHealth;

    public float MaxHealth => maxHealth;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        _lastPosition = transform.position;
    }

    protected virtual void Update()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (Time.deltaTime > 0)
        {
            _currentVelocity = (transform.position - _lastPosition) / Time.deltaTime;
        }
        _lastPosition = transform.position;
    }

    public abstract void TakeDamage(float damage);

    public virtual void ChangeHealth(float amount)
    {
        currentHealth += amount;
    }
}