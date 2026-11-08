namespace ClientPulse.Api.Clients;

public class Client
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Industry { get; set; }

    public required string AccountOwner { get; set; }
}
