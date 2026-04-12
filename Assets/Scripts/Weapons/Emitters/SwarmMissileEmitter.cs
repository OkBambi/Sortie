using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu(menuName = "Combat/Modules/Emitters/Swarm Missile")]
public class SwarmMissileEmitter : WeaponEmitter
{
    public int BaseDamage = 15;

    [Header("Volley Settings")]
    [Tooltip("The guaranteed number of missiles fired every time you pull the trigger.")]
    public int MissilesPerVolley = 12;
    public float DelayBetweenMissiles = 0.05f;

    [Header("Flight Dynamics")]
    public float MaxSpeed = 80f;
    public float Acceleration = 60f;
    public float InitialEjectSpeed = 15f;
    public float SpreadAngle = 60f;
    public float UpwardEjectBias = 1.5f;

    [Header("Itano Zigzag (The Circus)")]
    public float HomingDelay = 0.4f;
    public float TurnSpeed = 250f;
    public float ZigzagIntensity = 4f;
    public float ZigzagFrequency = 4f;
    public float SnapSharpness = 20f;

    [Header("Explosion")]
    public float ExplosionRadius = 5f;

    [Header("Visuals")]
    public GameObject MissilePrefab;
    public GameObject MuzzleFlashPrefab;
    public GameObject HitParticlePrefab;

    public override void Fire(WeaponInstance instance, float chargeModifier)
    {
        CombatSystem combatSys = instance.Context.Runner as CombatSystem;
        List<Transform> targets = combatSys != null ? combatSys.GetMultiTargets(instance) : new List<Transform>();

        if (targets == null || targets.Count == 0)
        {
            targets = new List<Transform>();
            if (instance.Context.CurrentTarget != null)
            {
                targets.Add(instance.Context.CurrentTarget);
            }
        }
        
        int totalMissilesToFire = Mathf.Max(MissilesPerVolley, targets.Count);

        if (totalMissilesToFire <= 1)
        {
            Transform singleTarget = targets.Count > 0 ? targets[0] : null;
            SpawnSingleMissile(instance, chargeModifier, singleTarget);
        }
        else
        {
            instance.Context.Runner.StartCoroutine(FireVolleyRoutine(instance, chargeModifier, targets, totalMissilesToFire));
        }
    }

    private IEnumerator FireVolleyRoutine(WeaponInstance instance, float chargeModifier, List<Transform> targets, int totalMissiles)
    {
        for (int i = 0; i < totalMissiles; i++)
        {
            Transform target = null;

            if (targets.Count > 0)
            {
                target = targets[i % targets.Count];
            }

            SpawnSingleMissile(instance, chargeModifier, target);

            if (DelayBetweenMissiles > 0)
            {
                yield return new WaitForSeconds(DelayBetweenMissiles);
            }
        }
    }

    private void SpawnSingleMissile(WeaponInstance instance, float chargeModifier, Transform target)
    {
        if (MuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(MuzzleFlashPrefab, instance.Context.MuzzlePoint.position, instance.Context.MuzzlePoint.rotation, instance.Context.MuzzlePoint);
            Destroy(flash, 0.05f);
        }

        int finalDamage = Mathf.RoundToInt(BaseDamage * Mathf.Max(0.1f, chargeModifier));

        ItanoCircusHandler.Instance.SpawnMissile(
            instance.Context.MuzzlePoint.position,
            instance.Context.MuzzlePoint.rotation,
            target,
            finalDamage,
            MaxSpeed,
            Acceleration,
            InitialEjectSpeed,
            SpreadAngle,
            UpwardEjectBias,
            HomingDelay,
            TurnSpeed,
            ZigzagIntensity,
            ZigzagFrequency,
            SnapSharpness,
            ExplosionRadius,
            MissilePrefab,
            HitParticlePrefab
        );
    }
}