using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Triggers/Multi-Lock")]
public class MultiLockTrigger : WeaponTrigger
{
    [Header("Multi-Lock Settings")]
    [Tooltip("Maximum number of separate targets (or stacked locks on one target)")]
    public int MaxLocks = 4;

    [Tooltip("Time required to acquire one lock")]
    public float LockTimePerTarget = 0.3f;

    [Tooltip("Automatically fire once all locks are acquired")]
    public bool AutoFireAtMaxLocks = false;

    public override void HandleInput(WeaponInstance instance, bool inputDown, bool inputHeld, bool inputUp)
    {
        float maxCharge = MaxLocks * LockTimePerTarget;

        if (inputHeld)
        {
            instance.CurrentCharge += Time.deltaTime;
            instance.CurrentCharge = Mathf.Clamp(instance.CurrentCharge, 0f, maxCharge);

            // Push the current charge state to the CombatSystem so it can generate visual UI templates
            if (instance.Context.Runner is CombatSystem combatSys)
            {
                combatSys.RegisterMultiLockCharge(instance, MaxLocks, LockTimePerTarget);
            }

            if (AutoFireAtMaxLocks && instance.CurrentCharge >= maxCharge)
            {
                instance.TryFire(1f);
                instance.CurrentCharge = 0f;
            }
        }
        else if (inputUp && instance.CurrentCharge > 0)
        {
            // Register one final time to guarantee locks are locked in on the exact frame we fire
            if (instance.Context.Runner is CombatSystem combatSys)
            {
                combatSys.RegisterMultiLockCharge(instance, MaxLocks, LockTimePerTarget);
            }

            float modifier = instance.CurrentCharge / maxCharge;
            instance.TryFire(modifier);
            instance.CurrentCharge = 0f;
        }
        else
        {
            // Reset charge if we weren't firing (or decay it if you want)
            instance.CurrentCharge = 0f;
        }
    }
}