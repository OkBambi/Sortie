public enum ResourceType
{
    Health,
    PrimaryAmmo,
    SecondaryAmmo,
    LeftAmmo,
    RightAmmo,
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
    int GetResourceCurrent(ResourceType type);
    int GetResourceMax(ResourceType type);
}