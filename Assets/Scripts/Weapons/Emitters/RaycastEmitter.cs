using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Raycast")]
public class RaycastEmitter : WeaponEmitter
{
    public int BaseDamage = 5;
    public float Range = 50f;
    public LayerMask HitLayer;
    public GameObject HitVFX;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        Vector3 direction = (instance.Context.TargetPoint - instance.Context.ShootingPoint.position).normalized;
        int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

        if (Physics.Raycast(instance.Context.ShootingPoint.position, direction, out RaycastHit hit, Range, HitLayer))
        {
            if (HitVFX != null)
            {
                Instantiate(HitVFX, hit.point, Quaternion.LookRotation(hit.normal));
            }

            if (hit.collider.TryGetComponent<IDamage>(out var damageable))
            {
                damageable.TakeDamage(finalDamage);
            }
        }
    }
}