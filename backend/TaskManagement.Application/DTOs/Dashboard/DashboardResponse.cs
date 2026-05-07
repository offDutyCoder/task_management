using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.DTOs.Dashboard;

public class AssigneeGroup
{
    public string AssigneeType { get; set; } = string.Empty; // "user" | "team"
    public int AssigneeId { get; set; }
    public string AssigneeName { get; set; } = string.Empty;
    public List<TaskListItem> Tasks { get; set; } = [];
}

public class DashboardResponse
{
    public List<TaskListItem> MyTasks { get; set; } = [];
    public List<AssigneeGroup> TeamTaskGroups { get; set; } = [];
}
