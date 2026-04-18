using UnityEngine;

/// <summary>
/// This is my EventHooks enum and it go PRAPRAPRAPRAPRAPRAPRA
/// </summary>
public enum EventHooks
{
    // DEFENSE
    /// <summary>Allows modification of damage BEFORE it subtracts health</summary>
    OnBeforeTakeDamage,
    /// <summary>Triggers AFTER health is lost</summary>
    OnTakeDamage,
    /// <summary>Triggers when taking a critical hit</summary>
    OnTakeCritDamage,
    /// <summary>Triggers when health drops below a certain threshold</summary>
    OnHealthLow,

    // OFFENSE
    /// <summary>Allows items to modify outgoing damage BEFORE it is sent to the target</summary>
    OnBeforeDealDamage,
    /// <summary>Triggers when damage registers</summary>
    OnDealDamage,
    /// <summary>Triggers when landing a critical hit</summary>
    OnDealCritDamage,

    // LIFE
    /// <summary>Triggers when the owner heals</summary>
    OnHeal,
    /// <summary>Triggers right before the owner dies</summary>
    OnDeath,
    /// <summary>People die when they are killed</summary>
    OnKill,

    // WEAPONS
    /// <summary>Triggers every time a weapon is fired</summary>
    OnAttackFired,
    /// <summary>Triggers when reloading starts (e.g. gain movement speed while reloading)</summary>
    OnReloadStart,
    /// <summary>Triggers when a weapon is fully reloaded (e.g. first shot does bonus damage)</summary>
    OnReloadComplete,
    /// <summary>Triggers when using primary, secondary, utility, or special skills</summary>
    OnSkillUsed,

    // MOBILITY
    /// <summary>Triggers when jumping (Hopoo Feathers mayhaps?)</summary>
    OnJump,
    /// <summary>Triggers when dashing</summary>
    OnDash,
    /// <summary>Triggers when touching the ground after a fall (HOLLIDAY BOUNCE PAD HOLLIDAY BOUNCE PAD)</summary>
    OnLand,
    /// <summary>Triggers while using boost</summary>
    OnThrusterUse,

    // ENVIRONMENT
    /// <summary>Triggers when interacting with.. an interactable :kirara_stare:</summary>
    OnInteract,
    /// <summary>Triggers when picking up a new item</summary>
    OnItemPickup,
    /// <summary>Triggers when a sortie begins</summary>
    OnStageStart,
    /// <summary>Triggers when a sortie is finished</summary>
    OnStageComplete,
    /// <summary>Triggers when leveling up</summary>
    OnLevelUp
}