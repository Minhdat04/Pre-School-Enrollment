using Microsoft.EntityFrameworkCore;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Child> Children { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure global query filter for soft delete
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                    var filter = System.Linq.Expressions.Expression.Lambda(
                        System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
                        parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.Role).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(u => u.Classroom)
                      .WithMany(c => c.Teachers)
                      .HasForeignKey(u => u.ClassroomId)
                      .OnDelete(DeleteBehavior.SetNull); 

                entity.HasMany(u => u.Children)
                      .WithOne(c => c.Parent)
                      .HasForeignKey(c => c.ParentId)
                      .OnDelete(DeleteBehavior.Cascade); 

                entity.HasMany(u => u.Applications)
                      .WithOne(a => a.CreatedBy)
                      .HasForeignKey(a => a.CreatedById)
                      .OnDelete(DeleteBehavior.Restrict); 

                entity.HasMany(u => u.Students)
                      .WithOne(s => s.Parent)
                      .HasForeignKey(s => s.ParentId)
                      .OnDelete(DeleteBehavior.Restrict); 

                // Một User có nhiều thanh toán
                entity.HasMany(u => u.Payments)
                      .WithOne(p => p.MadeBy)
                      .HasForeignKey(p => p.MadeById)
                      .OnDelete(DeleteBehavior.Restrict); 
            });

            modelBuilder.Entity<Application>(entity =>
            {
                entity.Property(e => e.Gender).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasMany(a => a.Payments)
                      .WithOne(p => p.Application)
                      .HasForeignKey(p => p.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade); 

                entity.HasOne(a => a.Child)
                      .WithMany() 
                      .HasForeignKey(a => a.ChildId)
                      .OnDelete(DeleteBehavior.SetNull); 
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.Property(e => e.Gender).HasConversion<string>();

                entity.HasOne(s => s.Classroom)
                      .WithMany(c => c.Students)
                      .HasForeignKey(s => s.ClassroomId)
                      .OnDelete(DeleteBehavior.SetNull); 
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.vnp_Amount).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<Classroom>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique(); 
            });

            modelBuilder.Entity<Blog>(entity =>
            {
                entity.HasMany(b => b.Tags)
                      .WithMany(t => t.Blogs)
                      .UsingEntity(j => j.ToTable("BlogTags")); 
            });

            modelBuilder.Entity<Child>(entity =>
            {
                entity.Property(e => e.Gender).HasConversion<string>();
            });
        }
    }
}