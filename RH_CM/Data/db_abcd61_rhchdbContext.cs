using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RH_CM.Models;

namespace RH_CM.Data
{
    public partial class db_abcd61_rhchdbContext : DbContext
    {
        public db_abcd61_rhchdbContext()
        {
        }

        public db_abcd61_rhchdbContext(DbContextOptions<db_abcd61_rhchdbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AspNetRole> AspNetRoles { get; set; } = null!;
        public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; } = null!;
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; } = null!;
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; } = null!;
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; } = null!;
        public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; } = null!;
        public virtual DbSet<CtCorrectanswer> CtCorrectanswers { get; set; } = null!;
        public virtual DbSet<CtCourse> CtCourses { get; set; } = null!;
        public virtual DbSet<CtCourseLevelMaterial> CtCourseLevelMaterials { get; set; } = null!;
        public virtual DbSet<CtCourseassignment> CtCourseassignments { get; set; } = null!;
        public virtual DbSet<CtCoursematerial> CtCoursematerials { get; set; } = null!;
        public virtual DbSet<CtCoursestatus> CtCoursestatuses { get; set; } = null!;
        public virtual DbSet<CtDeliverymode> CtDeliverymodes { get; set; } = null!;
        public virtual DbSet<CtDepartment> CtDepartments { get; set; } = null!;
        public virtual DbSet<CtLevelcourse> CtLevelcourses { get; set; } = null!;
        public virtual DbSet<CtOcupationcode> CtOcupationcodes { get; set; } = null!;
        public virtual DbSet<CtOption> CtOptions { get; set; } = null!;
        public virtual DbSet<CtOptiontype> CtOptiontypes { get; set; } = null!;
        public virtual DbSet<CtPermission> CtPermissions { get; set; } = null!;
        public virtual DbSet<CtPermissionsgroup> CtPermissionsgroups { get; set; } = null!;
        public virtual DbSet<CtPosition> CtPositions { get; set; } = null!;
        public virtual DbSet<CtQuestion> CtQuestions { get; set; } = null!;
        public virtual DbSet<CtResult> CtResults { get; set; } = null!;
        public virtual DbSet<CtSupervisor> CtSupervisors { get; set; } = null!;
        public virtual DbSet<CtTest> CtTests { get; set; } = null!;
        public virtual DbSet<CtThematicarea> CtThematicareas { get; set; } = null!;
        public virtual DbSet<CtThematiccourse> CtThematiccourses { get; set; } = null!;
        public virtual DbSet<CtVideomaterial> CtVideomaterials { get; set; } = null!;
        public virtual DbSet<SyCoursecompleted> SyCoursecompleteds { get; set; } = null!;
        public virtual DbSet<SyCoursemovement> SyCoursemovements { get; set; } = null!;
        public virtual DbSet<SyExcludedcourseassignment> SyExcludedcourseassignments { get; set; } = null!;
        public virtual DbSet<SyExternalevidence> SyExternalevidences { get; set; } = null!;
        public virtual DbSet<SyHeadcount> SyHeadcounts { get; set; } = null!;
        public virtual DbSet<SyUserAnswer> SyUserAnswers { get; set; } = null!;
        public virtual DbSet<SyUserDiagnostic> SyUserDiagnostics { get; set; } = null!;
        public virtual DbSet<VwUserAnswersResume> VwUserAnswersResumes { get; set; } = null!;
        public virtual DbSet<VwUserDiagnosticResume> VwUserDiagnosticResumes { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                optionsBuilder.UseSqlServer(configuration.GetConnectionString("ConexionSQL"));
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

            modelBuilder.Entity<CtCorrectanswer>(entity =>
            {
                entity.HasKey(e => e.PkAnswers)
                    .HasName("PK__CT_CORRE__5906CE16FD175DB8");

                entity.ToTable("CT_CORRECTANSWERS");

                entity.Property(e => e.PkAnswers).HasColumnName("PK_ANSWERS");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(250)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkOptions)
                    .IsUnicode(false)
                    .HasColumnName("FK_OPTIONS");

                entity.Property(e => e.FkQuestions).HasColumnName("FK_QUESTIONS");

                entity.Property(e => e.Lastupatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(250)
                    .HasColumnName("LASTUPDATEUSER");
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

            modelBuilder.Entity<CtCourseLevelMaterial>(entity =>
            {
                entity.HasKey(e => e.PkCourseLevelMaterial)
                    .HasName("PK__CT_COURS__D467EFDF0F505BF0");

                entity.ToTable("CT_COURSE_LEVEL_MATERIAL");

                entity.HasIndex(e => new { e.FkCourse, e.FkLevelCourse }, "IX_CLM_Course_Level");

                entity.HasIndex(e => e.FkCourseMaterial, "IX_CLM_Material");

                entity.HasIndex(e => new { e.FkCourse, e.FkLevelCourse, e.FkCourseMaterial }, "UX_CLM_Course_Level_Material")
                    .IsUnique();

                entity.Property(e => e.PkCourseLevelMaterial).HasColumnName("PK_CourseLevelMaterial");

                entity.Property(e => e.Available).HasDefaultValueSql("((1))");

                entity.Property(e => e.CreateDate).HasDefaultValueSql("(sysutcdatetime())");

                entity.Property(e => e.CreateUser).HasMaxLength(256);

                entity.Property(e => e.FkCourse).HasColumnName("FK_Course");

                entity.Property(e => e.FkCourseMaterial).HasColumnName("FK_CourseMaterial");

                entity.Property(e => e.FkLevelCourse).HasColumnName("FK_LevelCourse");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(256);
            });

            modelBuilder.Entity<CtCourseassignment>(entity =>
            {
                entity.HasKey(e => e.PkCourseAssignment);

                entity.ToTable("CT_COURSEASSIGNMENTS");

                entity.HasIndex(e => new { e.FkPosition, e.FkCourse, e.FkRequiredCourseLevels }, "UX_CtCourseassignments_PosCourseLevel")
                    .IsUnique();

                entity.Property(e => e.PkCourseAssignment).HasColumnName("PK_CourseAssignment");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser).HasMaxLength(50);

                entity.Property(e => e.FkCourse).HasColumnName("FK_Course");

                entity.Property(e => e.FkDeliveryMode).HasColumnName("FK_DeliveryMode");

                entity.Property(e => e.FkPosition).HasColumnName("FK_Position");

                entity.Property(e => e.FkRequiredCourseLevels).HasColumnName("FK_RequiredCourseLevels");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(50);
            });

            modelBuilder.Entity<CtCoursematerial>(entity =>
            {
                entity.HasKey(e => e.PkCoursematerial)
                    .HasName("PK__CT_COURS__D3564EC035FD0AE6");

                entity.ToTable("CT_COURSEMATERIAL");

                entity.Property(e => e.PkCoursematerial).HasColumnName("PK_COURSEMATERIAL");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser).HasMaxLength(50);

                entity.Property(e => e.File).HasColumnName("FILE");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(50);

                entity.Property(e => e.MaterialType)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("MATERIAL_TYPE");

                entity.Property(e => e.NameMaterial)
                    .HasMaxLength(255)
                    .HasColumnName("NAME_MATERIAL");

                entity.Property(e => e.UrlPath)
                    .HasMaxLength(500)
                    .HasColumnName("URL_PATH");
            });

            modelBuilder.Entity<CtCoursestatus>(entity =>
            {
                entity.HasKey(e => e.PkCoursestatus)
                    .HasName("PK_CT_COURSESTATUS_1");

                entity.ToTable("CT_COURSESTATUS");

                entity.Property(e => e.PkCoursestatus).HasColumnName("PK_COURSESTATUS");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.DescriptionCoursestatus)
                    .HasMaxLength(50)
                    .HasColumnName("DESCRIPTION_COURSESTATUS");

                entity.Property(e => e.Lastupatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPDATEUSER");
            });

            modelBuilder.Entity<CtDeliverymode>(entity =>
            {
                entity.HasKey(e => e.PkDeliverymode);

                entity.ToTable("CT_DELIVERYMODE");

                entity.Property(e => e.PkDeliverymode).HasColumnName("PK_DELIVERYMODE");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.DescriptionDeliverymode)
                    .HasMaxLength(50)
                    .HasColumnName("DESCRIPTION_DELIVERYMODE");

                entity.Property(e => e.Lastupatedate)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPDATEUSER");
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

            modelBuilder.Entity<CtLevelcourse>(entity =>
            {
                entity.HasKey(e => e.PkLevelcourse);

                entity.ToTable("CT_LEVELCOURSE");

                entity.Property(e => e.PkLevelcourse).HasColumnName("PK_LEVELCOURSE");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.DescripctionLevel)
                    .HasMaxLength(50)
                    .HasColumnName("DESCRIPCTION_LEVEL");

                entity.Property(e => e.Lastupatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPDATEUSER");
            });

            modelBuilder.Entity<CtOcupationcode>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CT_OCUPATIONCODE");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkPosition).HasColumnName("FK_POSITION");

                entity.Property(e => e.Lastupdatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPDATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPDATEUSER");

                entity.Property(e => e.Ocupationcode).HasColumnName("OCUPATIONCODE");

                entity.Property(e => e.PkOcupationcode)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("PK_OCUPATIONCODE");
            });

            modelBuilder.Entity<CtOption>(entity =>
            {
                entity.HasKey(e => e.PkOptions)
                    .HasName("PK__CT_OPTIO__B66109E91B9AFFB0");

                entity.ToTable("CT_OPTIONS");

                entity.Property(e => e.PkOptions).HasColumnName("PK_OPTIONS");

                entity.Property(e => e.Answer).HasColumnName("ANSWER");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(250)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkQuestions).HasColumnName("FK_QUESTIONS");

                entity.Property(e => e.Lastupatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(250)
                    .HasColumnName("LASTUPDATEUSER");

                entity.Property(e => e.Options).HasColumnName("OPTIONS");
            });

            modelBuilder.Entity<CtOptiontype>(entity =>
            {
                entity.HasKey(e => e.PkOptiontype);

                entity.ToTable("CT_OPTIONTYPE");

                entity.Property(e => e.PkOptiontype).HasColumnName("PK_OPTIONTYPE");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.DescriptionOptiontype)
                    .HasMaxLength(50)
                    .HasColumnName("DESCRIPTION_OPTIONTYPE");

                entity.Property(e => e.Lastupatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPDATEUSER");
            });

            modelBuilder.Entity<CtPermission>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CT_PERMISSION");

                entity.Property(e => e.FkPermissionGroup).HasColumnName("FK_PermissionGroup");

                entity.Property(e => e.FkRoleId)
                    .HasMaxLength(450)
                    .HasColumnName("FK_RoleId");

                entity.Property(e => e.PkPermission)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("PK_Permission");
            });

            modelBuilder.Entity<CtPermissionsgroup>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CT_PERMISSIONSGROUPS");

                entity.Property(e => e.ControllerName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.GroupName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PkPermissionGroup)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("PK_PermissionGroup");
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

            modelBuilder.Entity<CtQuestion>(entity =>
            {
                entity.HasKey(e => e.PkQuestions)
                    .HasName("PK__CT_QUEST__F76D2C55358E8D1C");

                entity.ToTable("CT_QUESTIONS");

                entity.Property(e => e.PkQuestions).HasColumnName("PK_QUESTIONS");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(250)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkTest).HasColumnName("FK_TEST");

                entity.Property(e => e.FkTypeOption).HasColumnName("FK_TYPE_OPTION");

                entity.Property(e => e.Lastupatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(250)
                    .HasColumnName("LASTUPDATEUSER");

                entity.Property(e => e.Question).HasColumnName("QUESTION");
            });

            modelBuilder.Entity<CtResult>(entity =>
            {
                entity.HasKey(e => e.PkResults)
                    .HasName("PK__CT_RESUL__2E829DC12CA8D00E");

                entity.ToTable("CT_RESULTS");

                entity.Property(e => e.PkResults).HasColumnName("PK_RESULTS");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(250)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkHeadcount).HasColumnName("FK_HEADCOUNT");

                entity.Property(e => e.FkTest).HasColumnName("FK_TEST");

                entity.Property(e => e.Lastupatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(250)
                    .HasColumnName("LASTUPDATEUSER");

                entity.Property(e => e.Results)
                    .HasColumnType("decimal(2, 2)")
                    .HasColumnName("RESULTS");
            });

            modelBuilder.Entity<CtSupervisor>(entity =>
            {
                entity.HasKey(e => e.PkSupervisorId);

                entity.ToTable("CT_SUPERVISOR");

                entity.Property(e => e.PkSupervisorId).HasColumnName("PK_SUPERVISOR_ID");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkDepartment).HasColumnName("FK_DEPARTMENT");

                entity.Property(e => e.FkHeadcount).HasColumnName("FK_HEADCOUNT");

                entity.Property(e => e.FkPosition).HasColumnName("FK_POSITION");

                entity.Property(e => e.Lastupdatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPDATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("LASTUPDATEUSER");
            });

            modelBuilder.Entity<CtTest>(entity =>
            {
                entity.HasKey(e => e.PkTest)
                    .HasName("PK__CT_TEST__599995560787B87A");

                entity.ToTable("CT_TEST");

                entity.Property(e => e.PkTest).HasColumnName("PK_TEST");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(250)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkCourse).HasColumnName("FK_COURSE");

                entity.Property(e => e.FkLevelcourse).HasColumnName("FK_LEVELCOURSE");

                entity.Property(e => e.Lastupatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(250)
                    .HasColumnName("LASTUPDATEUSER");

                entity.Property(e => e.TestName)
                    .HasMaxLength(250)
                    .HasColumnName("TEST_NAME");
            });

            modelBuilder.Entity<CtThematicarea>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CT_THEMATICAREA");

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

                entity.Property(e => e.PkThematicarea)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("PK_THEMATICAREA");

                entity.Property(e => e.ThematicCode).HasColumnName("THEMATIC_CODE");

                entity.Property(e => e.ThematicName)
                    .HasMaxLength(250)
                    .HasColumnName("THEMATIC_NAME");
            });

            modelBuilder.Entity<CtThematiccourse>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("CT_THEMATICCOURSE");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(50)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkCourse).HasColumnName("FK_COURSE");

                entity.Property(e => e.FkThematicarea).HasColumnName("FK_THEMATICAREA");

                entity.Property(e => e.Lastupdatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPDATEDATE");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUPDATEUSER");

                entity.Property(e => e.PkThematiccourse)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("PK_THEMATICCOURSE");
            });

            modelBuilder.Entity<CtVideomaterial>(entity =>
            {
                entity.HasKey(e => e.PkVideomaterial)
                    .HasName("PK__CT_VIDEO__6EBA570F8966A926");

                entity.ToTable("CT_VIDEOMATERIAL");

                entity.Property(e => e.PkVideomaterial).HasColumnName("PK_VIDEOMATERIAL");

                entity.Property(e => e.Available).HasColumnName("available");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("createdate");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("createuser");

                entity.Property(e => e.Lastupdatedate)
                    .HasColumnType("datetime")
                    .HasColumnName("lastupdatedate");

                entity.Property(e => e.Lastupdateuser)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("lastupdateuser");

                entity.Property(e => e.NameVideo)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("NAME_VIDEO");

                entity.Property(e => e.UrlPath)
                    .HasMaxLength(150)
                    .IsUnicode(false)
                    .HasColumnName("URL_PATH");
            });

            modelBuilder.Entity<SyCoursecompleted>(entity =>
            {
                entity.HasKey(e => e.PkCourseCompleted);

                entity.ToTable("SY_COURSECOMPLETED");

                entity.Property(e => e.PkCourseCompleted).HasColumnName("PK_CourseCompleted");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser).HasMaxLength(50);

                entity.Property(e => e.FkCourseAssignment).HasColumnName("FK_CourseAssignment");

                entity.Property(e => e.FkCourseStatus).HasColumnName("FK_CourseStatus");

                entity.Property(e => e.FkDeliveryMode).HasColumnName("FK_DeliveryMode");

                entity.Property(e => e.FkHeadcount).HasColumnName("FK_Headcount");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(50);
            });

            modelBuilder.Entity<SyCoursemovement>(entity =>
            {
                entity.HasKey(e => e.PkMovementCourse)
                    .HasName("PK__SY_COURS__76E4EDC702F5803C");

                entity.ToTable("SY_COURSEMOVEMENTS");

                entity.Property(e => e.PkMovementCourse).HasColumnName("PK_MovementCourse");

                entity.Property(e => e.CodeExam).HasColumnName("CODE_EXAM");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser).HasMaxLength(50);

                entity.Property(e => e.FkCourseAssignment).HasColumnName("FK_CourseAssignment");

                entity.Property(e => e.FkCourseStatus).HasColumnName("FK_CourseStatus");

                entity.Property(e => e.FkDeliveryMode).HasColumnName("FK_DeliveryMode");

                entity.Property(e => e.FkHeadcount).HasColumnName("FK_Headcount");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser).HasMaxLength(50);
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

            modelBuilder.Entity<SyExternalevidence>(entity =>
            {
                entity.HasKey(e => e.PkExternalEvidence)
                    .HasName("PK__SY_EXTER__8A61E2F3816BC57B");

                entity.ToTable("SY_EXTERNALEVIDENCE");

                entity.Property(e => e.PkExternalEvidence).HasColumnName("PK_ExternalEvidence");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.CreateUser)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.EvidenceFileName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.FkMovementCourse).HasColumnName("FK_MovementCourse");

                entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.LastUpdateUser)
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Score).HasColumnType("decimal(5, 2)");
            });

            modelBuilder.Entity<SyHeadcount>(entity =>
            {
                entity.HasKey(e => e.PkHeadcount)
                    .HasName("PK_SY_HeadCount");

                entity.ToTable("SY_HEADCOUNT");

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

                entity.Property(e => e.FkSupervisorId).HasColumnName("FK_SUPERVISOR_ID");

                entity.Property(e => e.LastName)
                    .HasMaxLength(50)
                    .HasColumnName("LAST_NAME");

                entity.Property(e => e.Lastupdate)
                    .HasColumnType("datetime")
                    .HasColumnName("LASTUPDATE");

                entity.Property(e => e.Lastuser)
                    .HasMaxLength(50)
                    .HasColumnName("LASTUSER");

                entity.Property(e => e.Layoffday)
                    .HasColumnType("date")
                    .HasColumnName("LAYOFFDAY");

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

                entity.Property(e => e.ZipCode).HasColumnName("ZIP_CODE");

                entity.Property(e => e.ZipCodesat).HasColumnName("ZIP_CODESAT");
            });

            modelBuilder.Entity<SyUserAnswer>(entity =>
            {
                entity.HasKey(e => e.PkUserAnswers)
                    .HasName("PK__SY_USER___33056E3502EB52C9");

                entity.ToTable("SY_USER_ANSWERS");

                entity.Property(e => e.PkUserAnswers).HasColumnName("PK_USER_ANSWERS");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.CodeExam).HasColumnName("CODE_EXAM");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(250)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkHeadcount).HasColumnName("FK_HEADCOUNT");

                entity.Property(e => e.FkOptionCorrected)
                    .HasMaxLength(50)
                    .HasColumnName("FK_OPTION_CORRECTED");

                entity.Property(e => e.FkOptionSelected)
                    .HasMaxLength(50)
                    .HasColumnName("FK_OPTION_SELECTED");

                entity.Property(e => e.FkQuestions).HasColumnName("FK_QUESTIONS");

                entity.Property(e => e.FkTest).HasColumnName("FK_TEST");
            });

            modelBuilder.Entity<SyUserDiagnostic>(entity =>
            {
                entity.HasKey(e => e.PkUserDiagnostic);

                entity.ToTable("SY_USER_DIAGNOSTIC");

                entity.Property(e => e.PkUserDiagnostic).HasColumnName("PK_USER_DIAGNOSTIC");

                entity.Property(e => e.Available).HasColumnName("AVAILABLE");

                entity.Property(e => e.CodeExam)
                    .HasColumnName("CODE_EXAM")
                    .HasDefaultValueSql("(NEXT VALUE FOR [dbo].[Seq_UserDiagnostic_CodeExam])");

                entity.Property(e => e.Createdate)
                    .HasColumnType("datetime")
                    .HasColumnName("CREATEDATE");

                entity.Property(e => e.Createuser)
                    .HasMaxLength(250)
                    .HasColumnName("CREATEUSER");

                entity.Property(e => e.FkHeadcount).HasColumnName("FK_HEADCOUNT");

                entity.Property(e => e.FkOptionCorrected)
                    .HasMaxLength(50)
                    .HasColumnName("FK_OPTION_CORRECTED");

                entity.Property(e => e.FkOptionSelected)
                    .HasMaxLength(50)
                    .HasColumnName("FK_OPTION_SELECTED");

                entity.Property(e => e.FkQuestions).HasColumnName("FK_QUESTIONS");

                entity.Property(e => e.FkTest).HasColumnName("FK_TEST");
            });

            modelBuilder.Entity<VwUserAnswersResume>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_UserAnswersResume");

                entity.Property(e => e.CodeExam).HasColumnName("CODE_EXAM");

                entity.Property(e => e.ExamDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<VwUserDiagnosticResume>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("vw_UserDiagnosticResume");

                entity.Property(e => e.CodeExam).HasColumnName("CODE_EXAM");

                entity.Property(e => e.ExamDate).HasColumnType("datetime");
            });

            modelBuilder.HasSequence("Seq_UserDiagnostic_CodeExam").HasMin(1);

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
