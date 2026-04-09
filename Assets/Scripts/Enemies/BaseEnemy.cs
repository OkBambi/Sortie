using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour, IDamage, ITarget
{
    [Header("Universal Stats")]
    public int maxHealth;
    protected int currentHealth;

    private Vector3 _lastPosition;
    private Vector3 _currentVelocity;

    public Transform Transform => transform;

    public Vector3 Velocity => _currentVelocity;

    public bool IsValid => currentHealth > 0 && gameObject.activeInHierarchy;

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
}