using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get([FromQuery] bool includeRecurring = false)
    {
        var result = await _dashboardService.GetDashboardAsync(GetCurrentUserId(), includeRecurring);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("ユーザーIDが取得できません");
        if (!int.TryParse(idClaim, out var userId))
            throw new InvalidOperationException($"ユーザーIDの形式が不正です: {idClaim}");
        return userId;
    }
}
