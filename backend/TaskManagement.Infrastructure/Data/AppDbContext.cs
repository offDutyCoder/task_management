using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Domain.Entities.TaskStatus> TaskStatuses => Set<Domain.Entities.TaskStatus>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();
    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
    public DbSet<TaskShare> TaskShares => Set<TaskShare>();
    public DbSet<LabelRequest> LabelRequests => Set<LabelRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RecurringTemplate> RecurringTemplates => Set<RecurringTemplate>();
    public DbSet<RecurringTemplateAssignee> RecurringTemplateAssignees => Set<RecurringTemplateAssignee>();
    public DbSet<RecurringTemplateLabel> RecurringTemplateLabels => Set<RecurringTemplateLabel>();
    public DbSet<RecurringTemplateShare> RecurringTemplateShares => Set<RecurringTemplateShare>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
