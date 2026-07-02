using ElectronicService.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Data;

public sealed class ElectronicDbContext : DbContext
{
    public ElectronicDbContext(DbContextOptions<ElectronicDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ElectronicDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}