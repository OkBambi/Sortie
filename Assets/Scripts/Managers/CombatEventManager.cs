using System;
using UnityEngine;

public static class CombatEventManager
{
    public static event Action<EventHooks, ItemEventData> OnCombatEvent;

    public static void FireEvent(EventHooks hook, ItemEventData data)
    {
        OnCombatEvent?.Invoke(hook, data);
    }
}