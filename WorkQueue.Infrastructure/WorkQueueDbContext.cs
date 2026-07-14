using Microsoft.EntityFrameworkCore;
using WorkQueue.Domain.Entities;

namespace WorkQueue.Infrastructure
{
    public class WorkQueueDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUserService;
        public WorkQueueDbContext(DbContextOptions<WorkQueueDbContext> options, ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<WorkItem> WorkItems { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TENANT ISOLATION
            // WHERE OrganizationId =
            modelBuilder.Entity<WorkItem>().HasQueryFilter(w => w.OrganizationId == _currentUserService.GetOrganizationId());
            modelBuilder.Entity<Comment>().HasQueryFilter(c => c.OrganizationId == _currentUserService.GetOrganizationId());
            modelBuilder.Entity<User>().HasQueryFilter(u => u.OrganizationId == _currentUserService.GetOrganizationId());

            // WorkItem references 
            modelBuilder.Entity<WorkItem>(entity =>
            {
                entity.HasKey(w => w.Id);

                entity.HasOne(w => w.Organization)
                    .WithMany(o => o.WorkItems)
                    .HasForeignKey(w => w.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(w => w.CreatedByUser)
                    .WithMany(u => u.CreatedWorkItems)
                    .HasForeignKey(w => w.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(w => w.AssigneeUser)
                    .WithMany(u => u.AssignedWorkItems)
                    .HasForeignKey(w => w.AssigneeUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // (optimist MS SQL)
                entity.Property(w => w.RowVersion)
                    .IsRowVersion();

                entity.HasIndex(w => new { w.OrganizationId, w.Status, w.Priority, w.AssigneeUserId })
                      .HasDatabaseName("IX_WorkItems_Tenant_Filters");
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.WorkItem)
                    .WithMany(w => w.Comments)
                    .HasForeignKey(c => c.WorkItemId)
                    .OnDelete(DeleteBehavior.Cascade); 

                entity.HasOne(c => c.CreatedByUser)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasOne(u => u.Organization)
                    .WithMany()
                    .HasForeignKey(u => u.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(u => u.Email)
                      .IsUnique();
            });
        }
    }
}
