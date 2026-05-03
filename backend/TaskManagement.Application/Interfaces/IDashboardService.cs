using TaskManagement.Application.DTOs.Dashboard;

namespace TaskManagement.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(int currentUserId, bool includeRecurring = false);
}
