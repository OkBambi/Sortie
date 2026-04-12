using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Melee")]
public class MeleeEmitter : WeaponEmitter
{
    public int BaseDamage = 25;
    public float HitRadius = 2f;
    public float ForwardOffset = 1f;
    public LayerMask EnemyLayer;
    public GameObject SwingVFXPrefab;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        if (SwingVFXPrefab != null)
        {
            GameObject vfx = Instantiate(SwingVFXPrefab, instance.Context.MuzzlePoint.position, 
                instance.Context.PlayerRoot.rotation, instance.Context.PlayerRoot);
            Destroy(vfx, 0.5f);
        }

        Vector3 hitCenter = instance.Context.PlayerRoot.position + (instance.Context.PlayerRoot.forward * ForwardOffset);
        Collider[] hitEnemies = Physics.OverlapSphere(hitCenter, HitRadius, EnemyLayer);

        int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<IDamage>(out var damageable))
            {
                damageable.TakeDamage(finalDamage);
            }
        }
    }
}