using System;
using System.Collections.Generic;
using Backendapi.Models;
using Microsoft.EntityFrameworkCore;

namespace Backendapi.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Assessment> Assessments { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Result> Results { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Option> Options { get; set; }

    public virtual DbSet<StudentAnswer> StudentAnswers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This will be used only if the DbContext is created without options
            // In normal operation, the connection string is provided through Program.cs
            optionsBuilder.UseSqlServer("Name=DefaultConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("Assessment");

            entity.Property(e => e.AssessmentId).ValueGeneratedNever();
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Course).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Assessment_Course");

            // Configure cascading delete for Results
            entity.HasMany(d => d.Results)
                .WithOne()
                .HasForeignKey("AssessmentId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure cascading delete for Questions
            entity.HasMany(d => d.Questions)
                .WithOne()
                .HasForeignKey("AssessmentId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course");

            entity.Property(e => e.CourseId).ValueGeneratedNever();
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MediaUrl)
                .HasColumnType("varchar(max)")
                .IsUnicode(false);
            entity.Property(e => e.CourseUrl)
                .HasColumnType("varchar(max)")
                .IsUnicode(false);
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Instructor).WithMany(p => p.Courses)
                .HasForeignKey(d => d.InstructorId)
                .HasConstraintName("FK_Course_User");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Result");

            entity.Property(e => e.ResultId).ValueGeneratedNever();
            entity.Property(e => e.AttemptDate).HasColumnType("datetime2");

            entity.HasOne(d => d.Assessment).WithMany(p => p.Results)
                .HasForeignKey(d => d.AssessmentId)
                .HasConstraintName("FK_Result_Assessment");

            entity.HasOne(d => d.User).WithMany(p => p.Results)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Result_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Question");

            entity.Property(e => e.QuestionId).ValueGeneratedNever();
            entity.Property(e => e.QuestionText)
                .IsRequired()
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.Assessment)
                .WithMany(p => p.Questions)
                .HasForeignKey(d => d.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Question_Assessment");
        });

        modelBuilder.Entity<Option>(entity =>
        {
            entity.ToTable("Option");

            entity.Property(e => e.OptionId).ValueGeneratedNever();
            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Question)
                .WithMany(p => p.Options)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Option_Question");
        });

        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.ToTable("StudentAnswer");

            entity.Property(e => e.AnswerId).ValueGeneratedNever();

            entity.HasOne(d => d.Result)
                .WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.ResultId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_StudentAnswer_Result");

            entity.HasOne(d => d.Question)
                .WithMany()
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StudentAnswer_Question");

            entity.HasOne(d => d.SelectedOption)
                .WithMany()
                .HasForeignKey(d => d.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_StudentAnswer_Option");
        });

        // Configure the many-to-many relationship between User and Course
        modelBuilder.Entity<User>()
            .HasMany(u => u.EnrolledCourses)
            .WithMany(c => c.EnrolledStudents)
            .UsingEntity(j => j.ToTable("UserEnrollments"));

        // Configure the one-to-many relationship between User (Instructor) and Course
        modelBuilder.Entity<Course>()
            .HasOne(c => c.Instructor)
            .WithMany(u => u.Courses)
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure the one-to-many relationship between Course and Assessment
        modelBuilder.Entity<Course>()
            .HasMany(c => c.Assessments)
            .WithOne(a => a.Course)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure the one-to-many relationship between User and Result
        modelBuilder.Entity<User>()
            .HasMany(u => u.Results)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
