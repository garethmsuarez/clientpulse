using ClientPulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClientPulse.Api.Clients;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> client)
    {
        client.Property(c => c.Name).HasMaxLength(200);
        client.Property(c => c.Industry).HasMaxLength(100);
        client.Property(c => c.AccountOwner).HasMaxLength(200);
        client.HasData(DemoSeedData.Clients);
    }
}
