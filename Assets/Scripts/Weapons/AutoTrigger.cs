using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Modules/Triggers/Auto")]
public class AutoTrigger : WeaponTrigger
{
    public float FireRate = 0.1f;
    public bool RequiresRepeatedClicks = false;

    public override void HandleInput(WeaponInstance instance, bool inputDown, bool inputHeld, bool inputUp)
    {
        bool shouldFire = RequiresRepeatedClicks ? inputDown : inputHeld;

        if (shouldFire && Time.time - instance.LastFireTime >= FireRate)
        {
            instance.TryFire(1f);
        }
    }
}