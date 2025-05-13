using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RH_CM.Models;

namespace RH_CM.Data
{
    public partial class RH_CHDBContext : DbContext
    {
        public RH_CHDBContext()
        {
        }

        public RH_CHDBContext(DbContextOptions<RH_CHDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AspNetRole> AspNetRoles { get; set; } = null!;
        public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; } = null!;
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; } = null!;
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; } = null!;
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; } = null!;
        public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; } = null!;
        public virtual DbSet<CtCourse> CtCourses { get; set; } = null!;
        public virtual DbSet<CtCourseassignment> CtCourseassignments { get; set; } = null!;
        public virtual DbSet<CtDepartment> CtDepartments { get; set; } = null!;
        public virtual DbSet<CtPosition> CtPositions { get; set; } = null!;
        public virtual DbSet<SyCoursecompleted> SyCoursecompleteds { get; set; } = null!;
        public virtual DbSet<SyExcludedcourseassignment> SyExcludedcourseassignments { get; set; } = null!;
        public virtual DbSet<SyHeadCount> SyHeadCounts { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=ch-mxmain1\\barcode; Database=RH_CHDB; User Id= sa; Password=Adm1n12;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AspNetRole>(entity =>
            {
                entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                    .IsUnique()
                    .HasFilter("([NormalizedName] IS NOT NULL)");

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.NormalizedName).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetRoleClaim>(entity =>
            {
                entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetRoleClaims)
                    .HasForeignKey(d => d.RoleId);
            });

            modelBuilder.Entity<AspNetUser>(entity =>
            {
                entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

                entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                    .IsUnique()
                    .HasFilter("([NormalizedUserName] IS NOT NULL)");

                entity.Property(e => e.Discriminator).HasDefaultValueSql("(N'')");

                entity.Property(e => e.Email).HasMaxLength(256);

                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

                entity.Property(e => e.NormalizedUserName).HasMaxLength(256);

                entity.Property(e => e.UserName).HasMaxLength(256);

                entity.HasMany(d => d.Roles)
                    .WithMany(p => p.Users)
                    .UsingEntity<Dictionary<string, object>>(
                        "AspNetUserRole",
                        l => l.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                        r => r.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                        j =>
                        {
                            j.HasKey("UserId", "RoleId");

                            j.ToTable("AspNetUserRoles");

                            j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                        });
            });

            modelBuilder.Entity<AspNetUserClaim>(entity =>
            {
                entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserClaims)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserLogin>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

                entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserLogins)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserToken>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserTokens)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<CtCourse>(entity =>
            {
                entity.HasKey(e => e.PkCourse);

                entity.ToTable("CT_COURSE");

                entity.Property(e => e.PkCourse).HasColumnName("PK_Course");

                entity.Property(e => e.CourseName).HasMaxLength(200);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser).HasMaxLength(50);

                entity.Property(e => e.Idcourse)
                    .HasMaxLength(50)
                    .HasColumnName("IDCourse");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(50);

                entity.Property(e => e.ManagementSystem).HasMaxLength(30);
            });

            modelBuilder.Entity<CtCourseassignment>(entity =>
            {
                entity.HasKey(e => e.PkCourseAssignment);

                entity.ToTable("CT_COURSEASSIGNMENTS");

                entity.Property(e => e.PkCourseAssignment).HasColumnName("PK_CourseAssignment");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser).HasMaxLength(50);

                entity.Property(e => e.FkCourse).HasColumnName("FK_Course");

                entity.Property(e => e.FkPosition).HasColumnName("FK_Position");

                entity.Property(e => e.FkRequiredCourseLevels).HasColumnName("FK_RequiredCourseLevels");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(50);
            });

            modelBuilder.Entity<CtDepartment>(entity =>
            {
                entity.HasKey(e => e.PkDepartment);

                entity.ToTable("CT_DEPARTMENT");

                entity.Property(e => e.PkDepartment).HasColumnName("PK_DEPARTMENT");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.Lastupdatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPDATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPDATEUSER");

                entity.Property(e => e.NameDeparment)
                    .HasMaxLength(50)
                    .HasColumnName("NAME_DEPARMENT");
            });

            modelBuilder.Entity<CtPosition>(entity =>
            {
                entity.HasKey(e => e.PkPosition);

                entity.ToTable("CT_POSITION");

                entity.Property(e => e.PkPosition).HasColumnName("PK_POSITION");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.Lastupdatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPDATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPDATEUSER");

                entity.Property(e => e.NamePosition)
                    .HasMaxLength(50)
                    .HasColumnName("NAME_POSITION");

                entity.Property(e => e.NamePositionEnglish)
                    .HasMaxLength(50)
                    .HasColumnName("NAME_POSITION_ENGLISH");
            });

            modelBuilder.Entity<SyCoursecompleted>(entity =>
            {
                entity.HasKey(e => e.PkCourseCompleted);

                entity.ToTable("SY_COURSECOMPLETED");

                entity.Property(e => e.PkCourseCompleted).HasColumnName("PK_CourseCompleted");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser).HasMaxLength(50);

                entity.Property(e => e.EnddateCourse)
                    .HasColumnType("date")
                    .HasColumnName("Enddate_Course");

                entity.Property(e => e.FkCourseAssignment).HasColumnName("FK_CourseAssignment");

                entity.Property(e => e.FkHeadcount).HasColumnName("FK_Headcount");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(50);

                entity.Property(e => e.StartdateCourse)
                    .HasColumnType("date")
                    .HasColumnName("Startdate_Course");
            });

            modelBuilder.Entity<SyExcludedcourseassignment>(entity =>
            {
                entity.HasKey(e => e.PkExcludedCourses);

                entity.ToTable("SY_EXCLUDEDCOURSEASSIGNMENTS");

                entity.Property(e => e.PkExcludedCourses).HasColumnName("PK_ExcludedCourses");

                entity.Property(e => e.Comment).HasMaxLength(250);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser).HasMaxLength(50);

                entity.Property(e => e.FkCourseAssignment).HasColumnName("FK_CourseAssignment");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(50);
            });

            modelBuilder.Entity<SyHeadCount>(entity =>
            {
                entity.HasKey(e => e.PkHeadcount);

                entity.ToTable("SY_HeadCount");

                entity.Property(e => e.PkHeadcount).HasColumnName("PK_HEADCOUNT");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Birthdate)
                    .HasColumnType("date")
                    .HasColumnName("BIRTHDATE");

                entity.Property(e => e.City)
                    .HasMaxLength(50)
                    .HasColumnName("CITY");

                entity.Property(e => e.ControlNumber).HasColumnName("CONTROL_NUMBER");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.Curp)
                    .HasMaxLength(20)
                    .HasColumnName("CURP");

                entity.Property(e => e.EducationLevel)
                    .HasMaxLength(50)
                    .HasColumnName("EDUCATION_LEVEL");

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .HasColumnName("EMAIL");

                entity.Property(e => e.FkDepartment).HasColumnName("FK_DEPARTMENT");

                entity.Property(e => e.FkPosition).HasColumnName("FK_POSITION");

                entity.Property(e => e.LastName)
                    .HasMaxLength(50)
                    .HasColumnName("LAST_NAME");

                entity.Property(e => e.Lastupdate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPDATE");

                entity.Property(e => e.Lastuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUSER");

                entity.Property(e => e.LevelEmployee)
                    .HasMaxLength(10)
                    .HasColumnName("LEVEL_EMPLOYEE");

                entity.Property(e => e.MaritalStatus)
                    .HasMaxLength(50)
                    .HasColumnName("MARITAL_STATUS");

                entity.Property(e => e.Names)
                    .HasMaxLength(50)
                    .HasColumnName("NAMES");

                entity.Property(e => e.Neighborhood)
                    .HasMaxLength(50)
                    .HasColumnName("NEIGHBORHOOD");

                entity.Property(e => e.Phone1)
                    .HasMaxLength(50)
                    .HasColumnName("PHONE1");

                entity.Property(e => e.Phone2)
                    .HasMaxLength(50)
                    .HasColumnName("PHONE2");

                entity.Property(e => e.Photo).HasColumnName("PHOTO");

                entity.Property(e => e.Rfc)
                    .HasMaxLength(15)
                    .HasColumnName("RFC");

                entity.Property(e => e.SecondName)
                    .HasMaxLength(50)
                    .HasColumnName("SECOND_NAME");

                entity.Property(e => e.Sex)
                    .HasMaxLength(6)
                    .HasColumnName("SEX");

                entity.Property(e => e.ShiftWork)
                    .HasMaxLength(2)
                    .HasColumnName("SHIFT_WORK");

                entity.Property(e => e.SocialSecurity)
                    .HasMaxLength(50)
                    .HasColumnName("SOCIAL_SECURITY");

                entity.Property(e => e.Specialization)
                    .HasMaxLength(100)
                    .HasColumnName("SPECIALIZATION");

                entity.Property(e => e.StarDate)
                    .HasColumnType("date")
                    .HasColumnName("STAR_DATE");

                entity.Property(e => e.Street)
                    .HasMaxLength(100)
                    .HasColumnName("STREET");

                entity.Property(e => e.Supervisor)
                    .HasMaxLength(20)
                    .HasColumnName("SUPERVISOR");

                entity.Property(e => e.SupervisorCode)
                    .HasMaxLength(20)
                    .HasColumnName("SUPERVISOR_CODE");

                entity.Property(e => e.ZipCode).HasColumnName("ZIP_CODE");

                entity.Property(e => e.ZipCodesat).HasColumnName("ZIP_CODESAT");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
