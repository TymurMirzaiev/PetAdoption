using Microsoft.EntityFrameworkCore;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure;

public class PetDbContext : DbContext
{
    public PetDbContext(DbContextOptions<PetDbContext> options) : base(options) {}

    public DbSet<Pet> Pets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pet>().HasData(
            new Pet { Id = Guid.NewGuid(), Name = "Bella", Type = "Dog", Status = PetStatus.Available },
            new Pet { Id = Guid.NewGuid(), Name = "Max", Type = "Cat", Status = PetStatus.Available }
        );
    }
}