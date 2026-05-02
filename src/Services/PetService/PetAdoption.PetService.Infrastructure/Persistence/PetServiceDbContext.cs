using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;
using PetAdoption.PetService.Domain.ValueObjects;
// Explicitly reference Allergy from Domain.ValueObjects to avoid ambiguity
using Allergy = PetAdoption.PetService.Domain.ValueObjects.Allergy;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetServiceDbContext : DbContext, IUnitOfWork
{
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<PetType> PetTypes => Set<PetType>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<PetSkip> PetSkips => Set<PetSkip>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AdoptionRequest> AdoptionRequests => Set<AdoptionRequest>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<PetInteraction> PetInteractions => Set<PetInteraction>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

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
            entity.Property(p => p.AdoptedAt);
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

            entity.OwnsMany(p => p.Media, media =>
            {
                media.ToTable("PetMedia");
                media.HasKey(m => m.Id);
                media.Property(m => m.Url).HasMaxLength(2000).IsRequired();
                media.Property(m => m.ContentType).HasMaxLength(100).IsRequired();
                media.Property(m => m.MediaType).HasConversion<string>().HasMaxLength(10);
            });

            entity.OwnsOne(p => p.MedicalRecord, mr =>
            {
                mr.Property(m => m.IsSpayedNeutered).HasColumnName("MedicalRecord_IsSpayedNeutered");
                mr.Property(m => m.SpayNeuterDate).HasColumnName("MedicalRecord_SpayNeuterDate");
                mr.Property(m => m.LastVetVisit).HasColumnName("MedicalRecord_LastVetVisit");
                mr.Property(m => m.UpdatedAt).HasColumnName("MedicalRecord_UpdatedAt");
                mr.Property(m => m.MicrochipId)
                    .HasConversion(
                        v => v == null ? null : v.Value,
                        v => v == null ? null : new MicrochipId(v))
                    .HasColumnName("MedicalRecord_MicrochipId")
                    .HasMaxLength(23);
                mr.Property(m => m.History)
                    .HasConversion(
                        v => v == null ? null : v.Value,
                        v => v == null ? null : new MedicalNotes(v))
                    .HasColumnName("MedicalRecord_History")
                    .HasMaxLength(5000);

                mr.OwnsMany(x => x.Vaccinations, v =>
                {
                    v.ToTable("PetVaccinations");
                    v.Property(vv => vv.VaccineType).HasMaxLength(100).IsRequired();
                    v.Property(vv => vv.Notes).HasMaxLength(500);
                });

                mr.OwnsMany(x => x.Allergies, a =>
                {
                    a.ToTable("PetAllergies");
                    a.Property(aa => aa.Value).HasMaxLength(100).IsRequired();
                });
            });
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

        modelBuilder.Entity<PetSkip>(entity =>
        {
            entity.ToTable("PetSkips");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.UserId, s.PetId }).IsUnique();
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

        modelBuilder.Entity<PetInteraction>(entity =>
        {
            entity.ToTable("PetInteractions");
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.Type).HasConversion<int>();
            entity.HasIndex(pi => new { pi.PetId, pi.Type });
            entity.HasIndex(pi => pi.CreatedAt);
            entity.HasIndex(pi => pi.PetId);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasKey(o => o.Id);
            entity.OwnsOne(o => o.Address, addr =>
            {
                addr.Property(a => a.Lat).HasColumnType("decimal(9,6)");
                addr.Property(a => a.Lng).HasColumnType("decimal(9,6)");
                addr.Property(a => a.Line1).HasMaxLength(200);
                addr.Property(a => a.City).HasMaxLength(100);
                addr.Property(a => a.Region).HasMaxLength(100);
                addr.Property(a => a.Country).HasMaxLength(100);
                addr.Property(a => a.PostalCode).HasMaxLength(20);
                addr.HasIndex(nameof(Address.Lat), nameof(Address.Lng));
            });
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.AdoptionRequestId).IsRequired();
            entity.Property(m => m.SenderUserId).IsRequired();
            entity.Property(m => m.SenderRole).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(m => m.Body).HasMaxLength(2000).IsRequired();
            entity.Property(m => m.SentAt).IsRequired();
            entity.Ignore(m => m.DomainEvents);
            entity.HasIndex(m => m.AdoptionRequestId);
            entity.HasIndex(m => m.SentAt);
        });
    }
}
