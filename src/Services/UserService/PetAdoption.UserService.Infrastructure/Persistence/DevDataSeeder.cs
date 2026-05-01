using Microsoft.Extensions.Logging;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

namespace PetAdoption.UserService.Infrastructure.Persistence;

/// <summary>
/// Seeds development data: platform admin, organizations, org users, and memberships.
/// Only runs in Development environment. Idempotent — skips if data already exists.
/// </summary>
public class DevDataSeeder
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DevDataSeeder> _logger;

    // Deterministic org IDs — shared with PetService DevDataSeeder
    public static readonly Guid HappyPawsOrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CityRescueOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CountrysideHavenOrgId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public DevDataSeeder(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IOrganizationMemberRepository memberRepository,
        IPasswordHasher passwordHasher,
        ILogger<DevDataSeeder> logger)
    {
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;
        _memberRepository = memberRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Checking if dev data needs to be seeded...");

        // Idempotency check: if platform admin exists, the org/admin block is done.
        var existingAdmin = await _userRepository.GetByEmailAsync(
            Email.From("platform-admin@petadoption.local"));

        if (existingAdmin is null)
        {
            _logger.LogInformation("Seeding platform admin and organizations...");

            await SeedPlatformAdminAsync();

            await SeedOrganizationAsync(
                HappyPawsOrgId,
                "Happy Paws Shelter",
                "happy-paws",
                "A no-kill shelter dedicated to finding forever homes");

            await SeedOrganizationAsync(
                CityRescueOrgId,
                "City Animal Rescue",
                "city-rescue",
                "Urban rescue center specializing in abandoned pets");

            await SeedOrganizationAsync(
                CountrysideHavenOrgId,
                "Countryside Haven",
                "countryside-haven",
                "Rural sanctuary for all animals");
        }
        else
        {
            _logger.LogInformation("Platform admin and organizations already seeded. Skipping.");
        }

        // Seed simple non-org user separately so it can be added to existing dev databases
        // without dropping data.
        await SeedSimpleUserAsync();

        _logger.LogInformation("Dev data seed pass complete.");
    }

    private async Task SeedPlatformAdminAsync()
    {
        var hashedPassword = _passwordHasher.HashPassword("Admin123!");
        var admin = User.Register(
            "platform-admin@petadoption.local",
            "Platform Administrator",
            hashedPassword,
            role: UserRole.PlatformAdmin);

        await _userRepository.SaveAsync(admin);
        _logger.LogInformation("Seeded platform admin: platform-admin@petadoption.local");
    }

    /// <summary>
    /// Seeds a plain non-org user for testing the browse/swipe/favorite/adopt flows
    /// from the perspective of an end user (no org admin/moderator powers).
    /// </summary>
    private async Task SeedSimpleUserAsync()
    {
        var email = Email.From("simple-user@petadoption.local");
        var existing = await _userRepository.GetByEmailAsync(email);
        if (existing is not null)
        {
            _logger.LogInformation("Simple user already exists. Skipping.");
            return;
        }

        var hashedPassword = _passwordHasher.HashPassword("User123!");
        var user = User.Register(
            "simple-user@petadoption.local",
            "Simple User",
            hashedPassword);

        await _userRepository.SaveAsync(user);
        _logger.LogInformation("Seeded simple user: simple-user@petadoption.local");
    }

    private async Task SeedOrganizationAsync(Guid orgId, string name, string slug, string description)
    {
        // Create organization with deterministic ID
        var org = Organization.Create(orgId, name, slug, description);
        await _organizationRepository.AddAsync(org);

        // Create org admin
        var adminHash = _passwordHasher.HashPassword("OrgAdmin123!");
        var orgAdmin = User.Register(
            $"admin@{slug}.local",
            $"{name} Admin",
            adminHash);
        await _userRepository.SaveAsync(orgAdmin);

        var adminMember = OrganizationMember.Create(orgId, orgAdmin.Id.Value, OrgRole.Admin);
        await _memberRepository.AddAsync(adminMember);

        // Create org moderator
        var modHash = _passwordHasher.HashPassword("OrgMod123!");
        var orgModerator = User.Register(
            $"moderator@{slug}.local",
            $"{name} Moderator",
            modHash);
        await _userRepository.SaveAsync(orgModerator);

        var modMember = OrganizationMember.Create(orgId, orgModerator.Id.Value, OrgRole.Moderator);
        await _memberRepository.AddAsync(modMember);

        // Create regular member (with Moderator org role)
        var memberHash = _passwordHasher.HashPassword("Member123!");
        var regularMember = User.Register(
            $"member@{slug}.local",
            $"{name} Member",
            memberHash);
        await _userRepository.SaveAsync(regularMember);

        var member = OrganizationMember.Create(orgId, regularMember.Id.Value, OrgRole.Moderator);
        await _memberRepository.AddAsync(member);

        _logger.LogInformation("Seeded organization '{Name}' with 3 members", name);
    }
}
