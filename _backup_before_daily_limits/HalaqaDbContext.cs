using System;
using System.Collections.Generic;
using Data_Access_Layer.Models;
using Microsoft.EntityFrameworkCore;

namespace Data_Access_Layer.Data;

public partial class HalaqaDbContext : DbContext
{
    public HalaqaDbContext(DbContextOptions<HalaqaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Center> Centers { get; set; }

    public virtual DbSet<HalaqaStudent> HalaqaStudents { get; set; }

    public virtual DbSet<Halaqat> Halaqats { get; set; }

    public virtual DbSet<MemorizationRecord> MemorizationRecords { get; set; }

    public virtual DbSet<MemorizationPlan> MemorizationPlans { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("Attendances_pkey");

            entity.HasIndex(e => new { e.StudentId, e.HalaqaId, e.AttendanceDate }, "Attendances_StudentId_HalaqaId_AttendanceDate_key").IsUnique();

            entity.Property(e => e.AttendanceId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Center).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Attendances_CenterId_fkey");

            entity.HasOne(d => d.Halaqa).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.HalaqaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Attendances_HalaqaId_fkey");

            entity.HasOne(d => d.RecordedByUser).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Attendances_RecordedByUserId_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Attendances_StudentId_fkey");
        });

        modelBuilder.Entity<Center>(entity =>
        {
            entity.HasKey(e => e.CenterId).HasName("Centers_pkey");

            entity.HasIndex(e => e.CenterCode, "Centers_CenterCode_key").IsUnique();

            entity.Property(e => e.CenterId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CenterCode).HasMaxLength(10);
            entity.Property(e => e.CenterName).HasMaxLength(150);
            entity.Property(e => e.CodeEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<HalaqaStudent>(entity =>
        {
            entity.HasKey(e => e.HalaqaStudentId).HasName("HalaqaStudents_pkey");

            entity.HasIndex(e => e.StudentId, "UX_HalaqaStudents_ActiveStudent")
                .IsUnique()
                .HasFilter("(\"EndDate\" IS NULL)");

            entity.Property(e => e.HalaqaStudentId).UseIdentityAlwaysColumn();

            entity.HasOne(d => d.Center).WithMany(p => p.HalaqaStudents)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("HalaqaStudents_CenterId_fkey");

            entity.HasOne(d => d.Halaqa).WithMany(p => p.HalaqaStudents)
                .HasForeignKey(d => d.HalaqaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("HalaqaStudents_HalaqaId_fkey");

            entity.HasOne(d => d.Student).WithOne(p => p.HalaqaStudent)
                .HasForeignKey<HalaqaStudent>(d => d.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("HalaqaStudents_StudentId_fkey");
        });

        modelBuilder.Entity<Halaqat>(entity =>
        {
            entity.HasKey(e => e.HalaqaId).HasName("Halaqat_pkey");

            entity.ToTable("Halaqat");

            entity.Property(e => e.HalaqaId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.HalaqaName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Center).WithMany(p => p.Halaqats)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Halaqat_CenterId_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Halaqats)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Halaqat_TeacherId_fkey");
        });

        modelBuilder.Entity<MemorizationRecord>(entity =>
        {
            entity.HasKey(e => e.MemorizationRecordId).HasName("MemorizationRecords_pkey");

            entity.HasIndex(e => new { e.StudentId, e.RecordDate }, "IX_MemorizationRecords_Student_Date");

            entity.Property(e => e.MemorizationRecordId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Source).HasDefaultValue((short)1);
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Center).WithMany(p => p.MemorizationRecords)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("MemorizationRecords_CenterId_fkey");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.MemorizationRecords)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("MemorizationRecords_CreatedByUserId_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.MemorizationRecords)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("MemorizationRecords_StudentId_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.MemorizationRecords)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("MemorizationRecords_TeacherId_fkey");
        });

        modelBuilder.Entity<MemorizationPlan>(entity =>
        {
            entity.HasKey(e => e.MemorizationPlanId).HasName("MemorizationPlans_pkey");

            entity.HasIndex(e => new { e.StudentId, e.StartDate }, "IX_MemorizationPlans_Student_StartDate");

            entity.Property(e => e.MemorizationPlanId).UseIdentityAlwaysColumn();
            entity.Property(e => e.DaysOfWeek)
                .HasColumnType("integer[]")
                .HasDefaultValueSql("ARRAY[0,2,4]::integer[]");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Center).WithMany()
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("MemorizationPlans_CenterId_fkey");

            entity.HasOne(d => d.Student).WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("MemorizationPlans_StudentId_fkey");

            entity.HasOne(d => d.CreatedByUser).WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("MemorizationPlans_CreatedByUserId_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("Roles_pkey");

            entity.HasIndex(e => e.RoleName, "Roles_RoleName_key").IsUnique();

            entity.Property(e => e.RoleId).UseIdentityAlwaysColumn();
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("Students_pkey");

            entity.HasIndex(e => e.UserId, "Students_UserId_key").IsUnique();

            entity.Property(e => e.StudentId).UseIdentityAlwaysColumn();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.Center).WithMany(p => p.Students)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Students_CenterId_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Students_UserId_fkey");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherId).HasName("Teachers_pkey");

            entity.HasIndex(e => e.UserId, "Teachers_UserId_key").IsUnique();

            entity.Property(e => e.TeacherId).UseIdentityAlwaysColumn();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.Center).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Teachers_CenterId_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Teachers_UserId_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("Users_pkey");

            entity.HasIndex(e => e.Email, "Users_Email_key").IsUnique();

            entity.Property(e => e.UserId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.RefreshTokenHash).HasMaxLength(100);
            entity.Property(e => e.RefreshTokenExpiresAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.RefreshTokenRevokedAt).HasColumnType("timestamp with time zone");

            entity.HasOne(d => d.Center).WithMany(p => p.Users)
                .HasForeignKey(d => d.CenterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Users_CenterId_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("Users_RoleId_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}


