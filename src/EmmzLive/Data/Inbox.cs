namespace EmmzLive.Data;

public sealed class Inbox
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Message> Messages { get; set; } = [];
}
