using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetServiceDbContext : DbContext
{
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<PetType> PetTypes => Set<PetType>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AdoptionRequest> AdoptionRequests => Set<AdoptionRequest>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    public PetServiceDbContext(DbContextOptions<PetServiceDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pet>(entity =>
        {
            entity.ToTable("Pets");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name)
                .HasConversion(v => v.Value, v => new PetName(v))
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(p => p.Breed)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : new PetBreed(v))
                .HasMaxLength(100);
            entity.Property(p => p.Age)
                .HasConversion(
                    v => v == null ? (int?)null : v.Months,
                    v => v == null ? null : new PetAge(v.Value))
                .HasColumnName("AgeMonths");
            entity.Property(p => p.Description)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => v == null ? null : new PetDescription(v))
                .HasMaxLength(2000);
            entity.Property(p => p.OrganizationId);
            entity.HasIndex(p => p.OrganizationId);
            entity.Property(p => p.Status).HasConversion<int>();
            entity.Property(p => p.Version).IsConcurrencyToken();
            entity.Ignore(p => p.DomainEvents);
            entity.Property(p => p.Tags)
                .HasField("_tags")
                .HasConversion(
                    v => JsonSerializer.Serialize(v.Select(t => t.Value).ToList(), (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v)
                        ? new List<PetTag>()
                        : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!
                            .Select(s => new PetTag(s)).ToList())
                .HasColumnName("Tags")
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);
        });

        modelBuilder.Entity<PetType>(entity =>
        {
            entity.ToTable("PetTypes");
            entity.HasKey(pt => pt.Id);
            entity.Property(pt => pt.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(pt => pt.Code).IsUnique();
            entity.Property(pt => pt.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.ToTable("Favorites");
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => new { f.UserId, f.PetId }).IsUnique();
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.ToTable("Announcements");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Title)
                .HasConversion(v => v.Value, v => new AnnouncementTitle(v))
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(a => a.Body)
                .HasConversion(v => v.Value, v => new AnnouncementBody(v))
                .HasMaxLength(5000)
                .IsRequired();
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("OutboxEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EventData).IsRequired();
            entity.Property(e => e.LastError).HasMaxLength(2000);
            entity.HasIndex(e => new { e.IsProcessed, e.OccurredOn });
        });

        modelBuilder.Entity<AdoptionRequest>(entity =>
        {
            entity.ToTable("AdoptionRequests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PetId).IsRequired();
            entity.Property(e => e.OrganizationId).IsRequired();
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.RejectionReason).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => new { e.UserId, e.PetId, e.Status })
                .HasFilter("[Status] = 'Pending'")
                .IsUnique();

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.PetId);
        });
    }
}
