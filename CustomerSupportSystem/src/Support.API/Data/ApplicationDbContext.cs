using Microsoft.EntityFrameworkCore;
using Support.Shared.Models;

namespace Support.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // =========================
    // DB SETS
    // =========================

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();

    // =========================
    // DATABASE CONFIGURATION
    // =========================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =====================================
        // USER
        // =====================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.Property(x => x.Role)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            // User -> Tickets created
            entity.HasMany(x => x.CreatedTickets)
                .WithOne(x => x.CreatedBy)
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Tickets assigned
            entity.HasMany(x => x.AssignedTickets)
                .WithOne(x => x.AssignedTo)
                .HasForeignKey(x => x.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            // User -> Ticket histories
            entity.HasMany(x => x.TicketHistories)
                .WithOne(x => x.ChangedBy)
                .HasForeignKey(x => x.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================
        // CATEGORY
        // =====================================

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            // Category -> Tickets
            entity.HasMany(x => x.Tickets)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =====================================
        // TICKET
        // =====================================

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.Priority)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired(false);

            entity.Property(x => x.ResolvedAt)
                .IsRequired(false);

            // Indexes
            entity.HasIndex(x => x.Status);

            entity.HasIndex(x => x.Priority);

            entity.HasIndex(x => x.CreatedAt);

            entity.HasIndex(x => x.CreatedById);

            entity.HasIndex(x => x.AssignedToId);

            entity.HasIndex(x => x.CategoryId);
        });

        // =====================================
        // TICKET HISTORY
        // =====================================

        modelBuilder.Entity<TicketHistory>(entity =>
        {
            entity.ToTable("TicketHistories");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Note)
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.Property(x => x.OldStatus)
                .HasConversion<int?>()
                .IsRequired(false);

            entity.Property(x => x.NewStatus)
                .HasConversion<int?>()
                .IsRequired(false);

            entity.Property(x => x.ChangedAt)
                .IsRequired();

            // Ticket -> Histories
            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.Histories)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> History changes
            entity.HasOne(x => x.ChangedBy)
                .WithMany(x => x.TicketHistories)
                .HasForeignKey(x => x.ChangedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(x => x.TicketId);

            entity.HasIndex(x => x.ChangedById);

            entity.HasIndex(x => x.ChangedAt);
        });
    }
}