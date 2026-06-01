namespace EmmzLive.Data;

public sealed class Message
{
    public Guid Id { get; set; }
    public Guid InboxId { get; set; }
    public string? SenderName { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }

    public Inbox Inbox { get; set; } = null!;
}
