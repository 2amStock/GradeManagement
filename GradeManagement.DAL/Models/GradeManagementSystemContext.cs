using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GradeManagement.DAL.Models;

public partial class GradeManagementSystemContext : DbContext
{
    public GradeManagementSystemContext()
    {
    }

    public GradeManagementSystemContext(DbContextOptions<GradeManagementSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<GradeCategory> GradeCategories { get; set; }

    public virtual DbSet<GradeItem> GradeItems { get; set; }

    public virtual DbSet<Mark> Marks { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentCourse> StudentCourses { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<UserAccount> UserAccounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-D3T65A6;Database=GradeManagementSystem;User Id=sa;Password=123;TrustServerCertificate=true;Trusted_Connection=SSPI;Encrypt=false;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Course__C92D71876359C084");

            entity.ToTable("Course");

            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.ClassCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LecturerId).HasColumnName("LecturerID");
            entity.Property(e => e.SubjectId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("SubjectID");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.Courses)
                .HasForeignKey(d => d.LecturerId)
                .HasConstraintName("FK_Course_Lecturer");

            entity.HasOne(d => d.Subject).WithMany(p => p.Courses)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Course__SubjectI__3D5E1FD2");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradeId).HasName("PK__Grade__54F87A37ED3919E5");

            entity.ToTable("Grade");

            entity.Property(e => e.GradeId).HasColumnName("GradeID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Course).WithMany(p => p.Grades)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__Grade__CourseID__2DE6D218");

            entity.HasOne(d => d.Student).WithMany(p => p.Grades)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Grade__StudentID__2EDAF651");
        });

        modelBuilder.Entity<GradeCategory>(entity =>
        {
            entity.HasKey(e => e.GradeCategoryId).HasName("PK__GradeCat__C86CC46AD2714310");

            entity.ToTable("GradeCategory");

            entity.Property(e => e.GradeCategoryName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<GradeItem>(entity =>
        {
            entity.HasKey(e => e.GradeItemId).HasName("PK__GradeIte__A40A4056AEA381C4");

            entity.Property(e => e.GradeItemId).HasColumnName("GradeItemID");
            entity.Property(e => e.GradeItemName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SubjectId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("SubjectID");

            entity.HasOne(d => d.GradeCategory).WithMany(p => p.GradeItems)
                .HasForeignKey(d => d.GradeCategoryId)
                .HasConstraintName("FK__GradeItem__Grade__2B0A656D");

            entity.HasOne(d => d.Subject).WithMany(p => p.GradeItems)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK_GradeItems_Subject");
        });

        modelBuilder.Entity<Mark>(entity =>
        {
            entity.HasKey(e => new { e.GradeId, e.GradeItemId }).HasName("PK__Mark__2EB8DE32D1D71EF7");

            entity.ToTable("Mark");

            entity.Property(e => e.GradeId).HasColumnName("GradeID");
            entity.Property(e => e.GradeItemId).HasColumnName("GradeItemID");
            entity.Property(e => e.Mark1).HasColumnName("Mark");

            entity.HasOne(d => d.Grade).WithMany(p => p.Marks)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mark__GradeID__31B762FC");

            entity.HasOne(d => d.GradeItem).WithMany(p => p.Marks)
                .HasForeignKey(d => d.GradeItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mark__GradeItemI__32AB8735");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__32C52A7986B9CAAD");

            entity.ToTable("Student");

            entity.HasIndex(e => e.Email, "UQ__Student__A9D10534E1E3AEAB").IsUnique();

            entity.HasIndex(e => e.RollNumber, "UQ__Student__E9F06F16709C8CC9").IsUnique();

            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.RollNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<StudentCourse>(entity =>
        {
            entity.HasKey(e => new { e.StudentId, e.CourseId });

            entity.ToTable("StudentCourse");

            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Course).WithMany(p => p.StudentCourses)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK_StudentCourse_Course");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentCourses)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_StudentCourse_Student");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subject__AC1BA388C8F0F800");

            entity.ToTable("Subject");

            entity.Property(e => e.SubjectId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("SubjectID");
            entity.Property(e => e.SubjectName).HasMaxLength(100);
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserAcco__1788CCACDF8B4CF8");

            entity.ToTable("UserAccount");

            entity.HasIndex(e => e.Email, "UQ__UserAcco__A9D10534E649C84E").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
