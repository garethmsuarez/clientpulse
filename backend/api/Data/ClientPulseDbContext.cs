using ClientPulse.Api.Assessments;
using ClientPulse.Api.Clients;
using ClientPulse.Api.Communications;
using Microsoft.EntityFrameworkCore;

namespace ClientPulse.Api.Data;

// Entity mapping lives with each slice (IEntityTypeConfiguration<T>);
// demo seed content lives in DemoSeedData.
public class ClientPulseDbContext(DbContextOptions<ClientPulseDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Communication> Communications => Set<Communication>();

    public DbSet<Assessment> Assessments => Set<Assessment>();

    public DbSet<AssessmentReview> AssessmentReviews => Set<AssessmentReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientPulseDbContext).Assembly);
}
