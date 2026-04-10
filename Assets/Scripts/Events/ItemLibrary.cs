using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A static database of all items in the game
/// </summary>
public static class ItemLibrary
{
    public static readonly Dictionary<string, ItemDefinition> Catalog = new Dictionary<string, ItemDefinition>
    {
        // ==========================================
        // COMMON ITEMS
        // ==========================================
        {
            "Crowbar_Equivalent", new ItemDefinition
            {
                Id = "Crowbar_Equivalent",
                Name = "Armor-Piercing Sabots",
                Tier = ItemTier.Common,
                Description = "Deal +50% damage to enemies above 90% health.",
                Hooks = new Dictionary<EventHooks, Action<ItemEventData>>
                {
                    { EventHooks.OnBeforeDealDamage, (eventData) =>
                        {
                            if (eventData is DamageEventData damageData)
                            {
                                damageData.DamageAmount *= 1.50f;
                            }
                        }
                    }
                }
            }
        },
        {
            "Heal_On_Kill", new ItemDefinition
            {
                Id = "Heal_On_Kill",
                Name = "Scrap Harvester",
                Tier = ItemTier.Common,
                Description = "Killing an enemy restores 10 Health.",
                Hooks = new Dictionary<EventHooks, Action<ItemEventData>>
                {
                    { EventHooks.OnKill, (eventData) =>
                        {
                            // if (eventData.Owner is Player player) player.Heal(10);
                        }
                    }
                }
            }
        },

        // ==========================================
        // UNCOMMON ITEMS
        // ==========================================
        {
            "Missile_Proc", new ItemDefinition
            {
                Id = "Missile_Proc",
                Name = "Micro-Missile Pod",
                Tier = ItemTier.Uncommon,
                Description = "10% chance on hit to fire a homing missile.",
                Hooks = new Dictionary<EventHooks, Action<ItemEventData>>
                {
                    { EventHooks.OnDealDamage, (eventData) =>
                        {
                            // 10% chance to trigger
                            if (UnityEngine.Random.value <= 0.10f)
                            {
                                Debug.Log($"[Micro-Missile] Fired a missile at {eventData.Target.Transform.name}!");
                                // ProjectileManager.Instance.SpawnProjectile(..., homingBehavior);
                            }
                        }
                    }
                }
            }
        },
        {
            "Speed_On_Reload", new ItemDefinition
            {
                Id = "Speed_On_Reload",
                Name = "Emergency Vents",
                Tier = ItemTier.Uncommon,
                Description = "Increases movement speed by 30% while reloading.",
                Hooks = new Dictionary<EventHooks, Action<ItemEventData>>
                {
                    { EventHooks.OnReloadStart, (eventData) =>
                        {
                            Debug.Log("[Emergency Vents] Speed increased!");
                            // playerMovement.ApplySpeedMultiplier(1.3f);
                        }
                    },
                    { EventHooks.OnReloadComplete, (eventData) =>
                        {
                            Debug.Log("[Emergency Vents] Speed returned to normal.");
                            // playerMovement.RemoveSpeedMultiplier(1.3f);
                        }
                    }
                }
            }
        }
    };
}