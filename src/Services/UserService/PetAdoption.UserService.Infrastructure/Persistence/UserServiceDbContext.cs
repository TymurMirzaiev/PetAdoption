using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.ValueObjects;

namespace PetAdoption.UserService.Infrastructure.Persistence;

public class UserServiceDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();

    public UserServiceDbContext(DbContextOptions<UserServiceDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .HasConversion(v => v.Value, v => UserId.From(v))
                .HasMaxLength(36);
            entity.Property(u => u.Email)
                .HasConversion(v => v.Value, v => Email.From(v))
                .HasMaxLength(255)
                .IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName)
                .HasConversion(v => v.Value, v => FullName.From(v))
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(u => u.Password)
                .HasConversion(
                    v => v == null ? null : v.HashedValue,
                    v => v == null ? null : Password.FromHash(v))
                .HasMaxLength(200);
            entity.Property(u => u.PhoneNumber)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : PhoneNumber.FromOptional(v))
                .HasMaxLength(15);
            entity.Property(u => u.Bio)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : Bio.FromOptional(v))
                .HasMaxLength(1000)
                .HasColumnName("Bio");
            entity.Property(u => u.Role).HasConversion<int>();
            entity.Property(u => u.Status).HasConversion<int>();
            entity.Property(u => u.ExternalProvider).HasMaxLength(50);
            entity.OwnsOne(u => u.Preferences, pref =>
            {
                pref.Property(p => p.PreferredPetType).HasMaxLength(50);
                pref.Property(p => p.PreferredAgeRange).HasMaxLength(50);
            });
            entity.Ignore(u => u.DomainEvents);
            entity.Ignore(u => u.HasPassword);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Id).HasMaxLength(36);
            entity.Property(rt => rt.UserId).HasMaxLength(36).IsRequired();
            entity.Property(rt => rt.Token).HasMaxLength(200).IsRequired();
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.HasIndex(rt => rt.UserId);
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("OutboxEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.EventType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EventData).IsRequired();
            entity.Property(e => e.RoutingKey).HasMaxLength(200).IsRequired();
            entity.Property(e => e.LastError).HasMaxLength(2000);
            entity.HasIndex(e => new { e.IsProcessed, e.CreatedAt });
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Name).HasMaxLength(100).IsRequired();
            entity.Property(o => o.Slug).HasMaxLength(50).IsRequired();
            entity.HasIndex(o => o.Slug).IsUnique();
            entity.Property(o => o.Description).HasMaxLength(500);
            entity.Property(o => o.Status).HasConversion<int>();
        });

        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.ToTable("OrganizationMembers");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.UserId).HasMaxLength(450).IsRequired();
            entity.Property(m => m.Role).HasConversion<int>();
            entity.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique();
        });
    }
}
