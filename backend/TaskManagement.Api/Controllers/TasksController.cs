using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<TaskListResponse>> GetAll([FromQuery] TaskFilterQuery filter)
    {
        var result = await _taskService.GetTasksAsync(filter, GetCurrentUserId());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDetailResponse>> Create([FromBody] CreateTaskRequest request)
    {
        var task = await _taskService.CreateTaskAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDetailResponse>> GetById(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id, GetCurrentUserId());
        return Ok(task);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskDetailResponse>> Update(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _taskService.UpdateTaskAsync(id, request, GetCurrentUserId());
        return Ok(task);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _taskService.DeleteTaskAsync(id, GetCurrentUserId());
        return NoContent();
    }

    [HttpPost("{id:int}/assignees")]
    public async Task<ActionResult<AssigneeDto>> AddAssignee(int id, [FromBody] AddAssigneeRequest request)
    {
        var assignee = await _taskService.AddAssigneeAsync(id, request.UserId, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id }, assignee);
    }

    [HttpDelete("{id:int}/assignees/{userId:int}")]
    public async Task<IActionResult> RemoveAssignee(int id, int userId)
    {
        await _taskService.RemoveAssigneeAsync(id, userId, GetCurrentUserId());
        return NoContent();
    }

    [HttpPost("{id:int}/shares")]
    public async Task<ActionResult<AssigneeDto>> AddShareUser(int id, [FromBody] AddAssigneeRequest request)
    {
        var shareUser = await _taskService.AddShareUserAsync(id, request.UserId, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id }, shareUser);
    }

    [HttpDelete("{id:int}/shares/{userId:int}")]
    public async Task<IActionResult> RemoveShareUser(int id, int userId)
    {
        await _taskService.RemoveShareUserAsync(id, userId, GetCurrentUserId());
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("ユーザーIDが取得できません");
        return int.Parse(idClaim);
    }
}
