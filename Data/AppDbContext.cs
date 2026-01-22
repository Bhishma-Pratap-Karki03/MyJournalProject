using Microsoft.EntityFrameworkCore;
using MyJournalProject.Entities;

namespace MyJournalProject.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Journal> Journals { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        try
        {
            Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating database: {ex.Message}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Email)
                  .IsUnique();
        });

        // Journal configuration
        modelBuilder.Entity<Journal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.PrimaryMood)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.SecondaryMood1)
                .HasMaxLength(50);

            entity.Property(e => e.SecondaryMood2)
                .HasMaxLength(50);

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("General");

            entity.Property(e => e.TagsJson)
                .HasDefaultValue("[]");

            entity.Property(e => e.EntryDate)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Foreign key relationship
            entity.HasOne(j => j.User)
                  .WithMany()
                  .HasForeignKey(j => j.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one journal per user per day
            entity.HasIndex(j => new { j.UserId, j.EntryDate })
                  .IsUnique()
                  .HasDatabaseName("IX_Journal_UserId_EntryDate");
        });
    }
}