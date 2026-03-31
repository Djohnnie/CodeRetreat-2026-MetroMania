using System.Text.Json;
using MetroMania.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MetroMania.Infrastructure.Sql.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DbSet<User> Users => Set<User>();
    public DbSet<Level> Levels => Set<Level>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionScore> SubmissionScores => Set<SubmissionScore>();
    public DbSet<SubmissionRender> SubmissionRenders => Set<SubmissionRender>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).IsClustered(false);
            entity.Property(e => e.SysId).UseIdentityColumn();
            entity.HasIndex(e => e.SysId).IsUnique().IsClustered(true);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.ApprovalStatus).HasConversion<string>();
            entity.Property(e => e.Language).HasMaxLength(10).HasDefaultValue("en");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.HasKey(e => e.Id).IsClustered(false);
            entity.Property(e => e.SysId).UseIdentityColumn();
            entity.HasIndex(e => e.SysId).IsUnique().IsClustered(true);
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

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).IsClustered(false);
            entity.Property(e => e.SysId).UseIdentityColumn();
            entity.HasIndex(e => e.SysId).IsUnique().IsClustered(true);
            entity.Property(e => e.Code).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Message).HasMaxLength(4000);
            entity.HasIndex(e => new { e.UserId, e.Version }).IsUnique();
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubmissionScore>(entity =>
        {
            entity.HasKey(e => e.Id).IsClustered(false);
            entity.Property(e => e.SysId).UseIdentityColumn();
            entity.HasIndex(e => e.SysId).IsUnique().IsClustered(true);
            entity.HasIndex(e => new { e.SubmissionId, e.LevelId }).IsUnique();
            entity.HasOne(e => e.Submission).WithMany(s => s.Scores)
                .HasForeignKey(e => e.SubmissionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Level).WithMany()
                .HasForeignKey(e => e.LevelId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubmissionRender>(entity =>
        {
            entity.HasKey(e => e.Id).IsClustered(false);
            entity.Property(e => e.SysId).UseIdentityColumn();
            entity.HasIndex(e => e.SysId).IsUnique().IsClustered(true);
            entity.HasIndex(e => new { e.SubmissionId, e.LevelId, e.Hour }).IsUnique();
            entity.Property(e => e.SvgContent).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasOne(e => e.Submission).WithMany()
                .HasForeignKey(e => e.SubmissionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Level).WithMany()
                .HasForeignKey(e => e.LevelId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}