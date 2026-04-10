public enum ResourceType
{
    Health,
    Ammo,
    Shield,
    ReloadProgress,
    Boost
}

/// <summary>
/// Any object (Player, Enemy, Item) that has a trackable stat should implement this.
/// </summary>
public interface IResourceProvider
{
    float GetResourcePercentage(ResourceType type);
}