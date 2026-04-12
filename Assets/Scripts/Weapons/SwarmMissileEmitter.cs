using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Swarm Missile")]
public class SwarmMissileEmitter : WeaponEmitter
{
    public int BaseDamage = 15;

    [Header("Volley Settings")]
    [Tooltip("How many missiles to fire per trigger pull")]
    public int MissilesPerVolley = 4;
    [Tooltip("Delay between each missile")]
    public float DelayBetweenMissiles = 0.05f;

    [Header("Flight Dynamics")]
    public float MaxSpeed = 80f;
    public float Acceleration = 60f;
    public float InitialEjectSpeed = 15f;
    [Tooltip("How wide the missiles spread when fired")]
    public float SpreadAngle = 60f;
    [Tooltip("Forces missiles to shoot upwards before tracking")]
    public float UpwardEjectBias = 1.5f;

    [Header("Itano Zigzag (The Circus)")]
    [Tooltip("Time before the missile actually starts chasing the target")]
    public float HomingDelay = 0.4f;
    public float TurnSpeed = 250f;
    [Tooltip("How far off the direct line to the target the missile pulls")]
    public float ZigzagIntensity = 8f;
    [Tooltip("How many times per second the missile changes its zigzag direction")]
    public float ZigzagFrequency = 4f;
    [Tooltip("How violently it snaps to the new trajectory (higher = sharper snap)")]
    public float SnapSharpness = 600f;

    [Header("Explosion")]
    public float ExplosionRadius = 8f;

    [Header("Visuals")]
    public GameObject MissilePrefab;
    public GameObject MuzzleFlashPrefab;
    public GameObject HitParticlePrefab;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        if (MissilesPerVolley <= 1)
        {
            SpawnSingleMissile(instance, chargeModifier);
        }
        else
        {
            instance.Context.Runner.StartCoroutine(FireVolleyRoutine(instance, chargeModifier));
        }
    }

    private System.Collections.IEnumerator FireVolleyRoutine(WeaponInstance instance, float chargeModifier)
    {
        for (int i = 0; i < MissilesPerVolley; i++)
        {
            SpawnSingleMissile(instance, chargeModifier);

            if (DelayBetweenMissiles > 0)
            {
                yield return new WaitForSeconds(DelayBetweenMissiles);
            }
        }
    }

    private void SpawnSingleMissile(WeaponInstance instance, float chargeModifier)
    {
        if (MuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(MuzzleFlashPrefab, instance.Context.MuzzlePoint.position, instance.Context.MuzzlePoint.rotation, instance.Context.MuzzlePoint);
            Destroy(flash, 0.05f);
        }

        if (MissilePrefab != null)
        {
            GameObject missileObj = Instantiate(MissilePrefab, instance.Context.MuzzlePoint.position, instance.Context.MuzzlePoint.rotation);
            var behavior = missileObj.AddComponent<ItanoCircusHandler>();

            int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

            behavior.Initialize(
                target: instance.Context.CurrentTarget,
                damage: finalDamage,
                maxSpeed: MaxSpeed,
                accel: Acceleration,
                ejectSpeed: InitialEjectSpeed,
                spread: SpreadAngle,
                upwardBias: UpwardEjectBias,
                delay: HomingDelay,
                turnSpd: TurnSpeed,
                zigInt: ZigzagIntensity,
                zigFreq: ZigzagFrequency,
                snapSharp: SnapSharpness,
                expRad: ExplosionRadius,
                hitVfx: HitParticlePrefab
            );
        }
    }
}