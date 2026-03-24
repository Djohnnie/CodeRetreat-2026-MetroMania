using System.Text.Json;
using MetroMania.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MetroMania.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DbSet<User> Users => Set<User>();
    public DbSet<Level> Levels => Set<Level>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.ApprovalStatus).HasConversion<string>();
            entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("en");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasIndex(e => e.SortOrder);

            entity.Property(e => e.LevelData)
                .HasColumnName("LevelDataJson")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<LevelData>(v, JsonOptions) ?? new LevelData())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(
                    new ValueComparer<LevelData>(
                        (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
                        v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
                        v => JsonSerializer.Deserialize<LevelData>(JsonSerializer.Serialize(v, JsonOptions), JsonOptions)!));
        });
    }
}