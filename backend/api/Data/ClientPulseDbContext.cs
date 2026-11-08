using ClientPulse.Api.Clients;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Data;

public class ClientPulseDbContext(DbContextOptions<ClientPulseDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(client =>
        {
            client.Property(c => c.Name).HasMaxLength(200);
            client.Property(c => c.Industry).HasMaxLength(100);
            client.HasData(SeedClients);
        });
    }

    // Synthetic demo clients only; not real organisations.
    public static readonly IReadOnlyList<Client> SeedClients =
    [
        new() { Id = 1, Name = "Northwind Logistics", Industry = "Logistics" },
        new() { Id = 2, Name = "Bluefin Health Partners", Industry = "Healthcare" },
        new() { Id = 3, Name = "Cedar Ridge Manufacturing", Industry = "Manufacturing" },
        new() { Id = 4, Name = "Lumen Retail Group", Industry = "Retail" },
        new() { Id = 5, Name = "Harbourview Financial", Industry = "Financial Services" },
    ];
}
