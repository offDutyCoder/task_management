using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Statuses;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/statuses")]
[Authorize]
public class StatusesController : ControllerBase
{
    private readonly IStatusService _statusService;

    public StatusesController(IStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StatusResponse>>> GetAll()
    {
        var statuses = await _statusService.GetAllAsync();
        return Ok(statuses);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StatusResponse>> GetById(int id)
    {
        var status = await _statusService.GetByIdAsync(id);
        return Ok(status);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StatusResponse>> Create([FromBody] CreateStatusRequest request)
    {
        var status = await _statusService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = status.Id }, status);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StatusResponse>> Update(int id, [FromBody] UpdateStatusRequest request)
    {
        var status = await _statusService.UpdateAsync(id, request);
        return Ok(status);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _statusService.DeleteAsync(id);
        return NoContent();
    }
}
