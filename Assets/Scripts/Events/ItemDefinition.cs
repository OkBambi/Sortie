using System;
using System.Collections.Generic;

public enum ItemTier
{
    Common,
    Uncommon,
    Legendary,
    Boss
}

public class ItemDefinition
{
    public string Id;
    public string Name;
    public string Description;
    public ItemTier Tier;

    //[EventHooks] -> [Function]
    public Dictionary<EventHooks, Action<ItemEventData>> Hooks = new Dictionary<EventHooks, Action<ItemEventData>>();
}