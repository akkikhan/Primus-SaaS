using Microsoft.EntityFrameworkCore;
using PrimusSaaS.Portal.Api.Models;

namespace PrimusSaaS.Portal.Api.Data;

public class PortalDbContext : DbContext
{
    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Module> Modules { get; set; } = null!;
    public DbSet<ModuleVersion> ModuleVersions { get; set; } = null!;
    public DbSet<Application> Applications { get; set; } = null!;
    public DbSet<ApplicationModule> ApplicationModules { get; set; } = null!;
    public DbSet<PackageRegistryMapping> PackageRegistryMappings { get; set; } = null!;
    public DbSet<WebhookRequest> WebhookRequests { get; set; } = null!;
    public DbSet<AppUser> AppUsers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        // AppUser entity (End-users of client apps)
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApplicationId, e.Username }).IsUnique(); // Unique username per app
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();

            entity.HasOne(e => e.Application)
                .WithMany() // No navigation property back needed for now
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Module entity
        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.ModuleKey).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ModuleKey).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
        });

        // ModuleVersion entity
        modelBuilder.Entity<ModuleVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ModuleId, e.Version }).IsUnique();
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ReleaseNotes).IsRequired();
            entity.Property(e => e.SupportedStacksJson).IsRequired();

            entity.HasOne(e => e.Module)
                .WithMany(m => m.Versions)
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Application entity
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PrimusClientId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PrimusClientId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ClientSecretHash).IsRequired().HasMaxLength(200);
            entity.Property(e => e.JwtSigningKey).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientSecretLastRotatedAt);

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Applications)
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationModule entity
        modelBuilder.Entity<ApplicationModule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ApplicationId, e.ModuleId }).IsUnique();
            entity.Property(e => e.ConfigJson).IsRequired();

            entity.HasOne(e => e.Application)
                .WithMany(a => a.ApplicationModules)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Module)
                .WithMany(m => m.ApplicationModules)
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ModuleVersion)
                .WithMany(mv => mv.ApplicationModules)
                .HasForeignKey(e => e.ModuleVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WebhookRequest entity
        modelBuilder.Entity<WebhookRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.RegistryType, e.PackageName });
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RegistryType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.Signature).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.ResponseBody).IsRequired();
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default admin user (password: Admin123!)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Email = "admin@primussaas.com",
            PasswordHash = "$2a$11$1cpBqvDSeWEpe8eDpouWDude7DsvSAJ6wtI9Ja4guFVMFPvzZmAuO", // BCrypt hash of Admin123!
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Seed IdentityValidator module
        modelBuilder.Entity<Module>().HasData(new Module
        {
            Id = 1,
            Name = "IdentityValidator",
            ModuleKey = "identity-validator",
            Description = "Authentication module supporting Local JWT and Azure AD OIDC validation"
        });

        // Seed initial version
        modelBuilder.Entity<ModuleVersion>().HasData(new ModuleVersion
        {
            Id = 1,
            ModuleId = 1,
            Version = "1.0.0",
            IsBreakingChange = false,
            ReleaseNotes = "Initial release of IdentityValidator module",
            SupportedStacksJson = "[\"DotNet\",\"NodeJS\"]",
            ReleasedAt = DateTime.UtcNow
        },
        new ModuleVersion
        {
            Id = 2,
            ModuleId = 1,
            Version = "1.1.0",
            IsBreakingChange = false,
            ReleaseNotes = "Added JWKS caching and improved Azure AD validation defaults",
            Changelog = "Added: in-memory JWKS cache with configurable TTL\nChanged: default audience parsing now trims api:// prefix\nFixed: null reference when openid config is temporarily unavailable",
            SupportedStacksJson = "[\"DotNet\",\"NodeJS\"]",
            ReleasedAt = DateTime.UtcNow.AddDays(7)
        });

        // Seed package registry mapping for npm
        modelBuilder.Entity<PackageRegistryMapping>().HasData(
            new PackageRegistryMapping
            {
                Id = 1,
                ModuleId = 1,
                RegistryType = "npm",
                PackageName = "primus-identity-validator",
                CreatedAt = DateTime.UtcNow
            },
            new PackageRegistryMapping
            {
                Id = 2,
                ModuleId = 1,
                RegistryType = "nuget",
                PackageName = "PrimusSaaS.Identity.Validator",
                CreatedAt = DateTime.UtcNow
            });
    }
}
