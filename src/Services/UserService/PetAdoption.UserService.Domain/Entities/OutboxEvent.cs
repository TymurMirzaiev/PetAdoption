namespace PetAdoption.UserService.Domain.Entities;

public class OutboxEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; private set; }
    public bool IsProcessed { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    public void MarkProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        LastError = error;
    }
}
