using IdentityHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Api.Data;

/// <summary>EF Core database context for IdentityHub entities.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<JiraConfiguration> JiraConfigurations => Set<JiraConfiguration>();
    public DbSet<TicketReference> TicketReferences => Set<TicketReference>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<JiraConfiguration>(e =>
        {
            e.HasIndex(j => j.UserId).IsUnique();
            e.HasOne(j => j.User)
             .WithOne(u => u.JiraConfiguration)
             .HasForeignKey<JiraConfiguration>(j => j.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketReference>(e =>
        {
            e.HasOne(t => t.User)
             .WithMany(u => u.TicketReferences)
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.UserId, t.ProjectKey });
        });

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasIndex(a => a.KeyHash).IsUnique();
            e.HasOne(a => a.User)
             .WithMany(u => u.ApiKeys)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
