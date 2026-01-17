using Microsoft.EntityFrameworkCore;
using MyJournalProject.Entities;

namespace MyJournalProject.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        // Ensure database is created
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
                .HasMaxLength(256); // For SHA256 hash

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on email
            entity.HasIndex(e => e.Email)
                  .IsUnique();

            // Add default value for CreatedAt
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("datetime('now')");
        });

        // SQLite specific configuration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Fallback configuration
            var databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyJournalProject",
                "myjournal.db");

            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }
    }
}