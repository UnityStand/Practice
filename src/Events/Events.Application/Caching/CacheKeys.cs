namespace Events.Application.Caching;

public static class CacheKeys
{
    public const string TopEvents = "events:top10";
    public static string Event(Guid id) => $"event:{id}";
}
