using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour, IDamage, ITarget, IHealth, IResourceProvider
{
    [Header("Universal Stats")]
    public float maxHealth;
    protected float currentHealth;

    private Vector3 _lastPosition;
    private Vector3 _currentVelocity;

    public Transform Transform => transform != null ? transform : null;

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

    public float GetResourcePercentage(ResourceType type)
    {
        if (type == ResourceType.Health) { return currentHealth / maxHealth; }
        else return 0;
    }

    public int GetResourceCurrent(ResourceType type)
    {
        throw new System.NotImplementedException();
    }

    public int GetResourceMax(ResourceType type)
    {
        throw new System.NotImplementedException();
    }
}