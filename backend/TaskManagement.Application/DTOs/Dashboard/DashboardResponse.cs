using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.DTOs.Dashboard;

public class DashboardResponse
{
    public List<TaskListItem> MyTasks { get; set; } = [];
    public List<TaskListItem> TeamTasks { get; set; } = [];
}
