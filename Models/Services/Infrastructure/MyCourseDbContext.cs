using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MyCourse.Models.Enums;

namespace MyCourse.Models.Entities.Services.Infrastructure
{
    public partial class MyCourseDbContext : IdentityDbContext
    {
        public MyCourseDbContext(DbContextOptions<MyCourseDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Course> Courses { get; set; }
        public virtual DbSet<Lesson> Lessons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.HasAnnotation("ProductVersion", "2.2.0-rtm-35687");
            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("Courses");//superfluo se la tabella si chiama come la prop che espone il set
                entity.HasKey(course => course.Id);//superfluo se la prop si chiama id o coursesId

                entity.HasIndex(course => course.Title).IsUnique();
                entity.Property(course => course.RowVersion).IsRowVersion();
                entity.Property(course => course.Status).HasConversion<String>();

                //mapping per gli owned types
                entity.OwnsOne(course => course.CurrentPrice, builder =>
                {
                    builder.Property(money => money.Currency)
                        .HasConversion<string>()
                        .HasColumnName("CurrentPrice_Currency");//superfluo 
                    builder.Property(money => money.Amount)
                        .HasColumnName("CurrentPrice_Amount")
                        .HasConversion<float>();//superfluo nel nostro caso
                });
                entity.OwnsOne(course => course.FullPrice, builder =>
                {
                    builder.Property(money => money.Currency)
                        .HasConversion<string>();
                    builder.Property(money => money.Amount)
                        .HasConversion<float>(); //Questo indica al meccanismo delle migration che la colonna della tabella dovrà essere creata di tipo numerico
                });

                //mapping per le relazioni
                entity.HasMany(course => course.Lessons)
                      .WithOne(lesson => lesson.Course)
                      .HasForeignKey(lesson => lesson.CourseId); // superflua se la prop si chiama courseId

                //Global Query Filter
                entity.HasQueryFilter(course => course.Status != CourseStatus.Deleted);

                #region Mapping generato automaticamente dal tool di reverse engeniring
                /*
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasColumnType("TEXT (100)");

                entity.Property(e => e.CurrentPriceAmount)
                    .IsRequired()
                    .HasColumnName("CurrentPrice_Amount")
                    .HasColumnType("NUMERIC")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CurrentPriceCurrency)
                    .IsRequired()
                    .HasColumnName("CurrentPrice_Currency")
                    .HasColumnType("TEXT (3)")
                    .HasDefaultValueSql("'EUR'");

                entity.Property(e => e.Description).HasColumnType("TEXT (10000)");

                entity.Property(e => e.Email).HasColumnType("TEXT (100)");

                entity.Property(e => e.FullPriceAmount)
                    .IsRequired()
                    .HasColumnName("FullPrice_Amount")
                    .HasColumnType("NUMERIC")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.FullPriceCurrency)
                    .IsRequired()
                    .HasColumnName("FullPrice_Currency")
                    .HasColumnType("TEXT (3)")
                    .HasDefaultValueSql("'EUR'");

                entity.Property(e => e.ImagePath).HasColumnType("TEXT (100)");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnType("TEXT (100)");       
            */
            });
            #endregion


            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.Property(lesson => lesson.RowVersion).IsRowVersion();
                entity.Property(lesson => lesson.Order).HasDefaultValue(1000).ValueGeneratedNever();
                
                /*entity.HasOne(lesson => lesson.Course)
                      .WithMany(course => course.Lessons);*/
                #region Mapping generato automaticamente dal tool di reverse engeniring
                /*
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Description).HasColumnType("TEXT (10000)");

                entity.Property(e => e.Duration)
                    .IsRequired()
                    .HasColumnType("TEXT (8)")
                    .HasDefaultValueSql("'00:00:00'");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnType("TEXT (100)");

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.Lessons)
                    .HasForeignKey(d => d.CourseId);
            */
                #endregion
            });
        }
    }
}
