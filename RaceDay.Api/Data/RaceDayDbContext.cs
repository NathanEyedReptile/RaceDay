
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<SupportingDocument> SupportingDocuments => Set<SupportingDocument>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("ROLE");
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName).HasColumnName("role_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USER");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.ProfilePic).HasColumnName("profile_pic");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("EVENT");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.Organiser).HasColumnName("organiser");
            entity.Property(e => e.EventName).HasColumnName("event_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EventDate).HasColumnName("event_date").HasColumnType("date");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.Distance).HasColumnName("distance").HasColumnType("decimal(5,2)");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.BannerImage).HasColumnName("banner_image");
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("ENROLMENT");
            entity.HasKey(e => e.EnrId);
            entity.Property(e => e.EnrId).HasColumnName("enr_id");
            entity.Property(e => e.Participant).HasColumnName("participant");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.EnrDate).HasColumnName("enr_date").HasColumnType("date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("CATEGORY");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.CategoryName).HasColumnName("category_name");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<SupportingDocument>(entity =>
        {
            entity.ToTable("SUPPORTING DOCUMENT");
            entity.HasKey(e => e.DocumentId);
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.EnrId).HasColumnName("enr_id");
            entity.Property(e => e.FileName).HasColumnName("file_name");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.ToTable("RACE RESULT");
            entity.HasKey(e => e.ResultId);
            entity.Property(e => e.ResultId).HasColumnName("result_id");
            entity.Property(e => e.EnrId).HasColumnName("enr_id");
            entity.Property(e => e.FinishTime).HasColumnName("finish_time").HasColumnType("time");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.Published).HasColumnName("published");
        });
    }
}
