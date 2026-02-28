using IdentityHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<JiraConfiguration> JiraConfigurations => Set<JiraConfiguration>();
    public DbSet<TicketReference> TicketReferences => Set<TicketReference>();

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
    }
}
