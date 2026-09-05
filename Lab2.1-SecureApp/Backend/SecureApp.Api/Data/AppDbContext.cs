using Microsoft.EntityFrameworkCore;
using SecureApp.Api.Models;

namespace SecureApp.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ConfidentialRecord> ConfidentialRecords => Set<ConfidentialRecord>();
    public DbSet<PublicRecord> PublicRecords => Set<PublicRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<ConfidentialRecord>()
            .HasOne(r => r.Owner)
            .WithMany(u => u.ConfidentialRecords)
            .HasForeignKey(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PublicRecord>()
            .HasOne(r => r.Owner)
            .WithMany(u => u.PublicRecords)
            .HasForeignKey(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
