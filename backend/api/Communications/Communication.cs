using ClientPulse.Api.Clients;

namespace ClientPulse.Api.Communications;

public class Communication
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public Client? Client { get; set; }

    public CommunicationType Type { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
