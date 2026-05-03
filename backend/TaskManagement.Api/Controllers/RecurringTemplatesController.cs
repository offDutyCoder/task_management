using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.RecurringTemplates;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/recurring-templates")]
[Authorize(Roles = "Admin")]
public class RecurringTemplatesController : ControllerBase
{
    private readonly IRecurringTaskService _recurringTaskService;

    public RecurringTemplatesController(IRecurringTaskService recurringTaskService)
    {
        _recurringTaskService = recurringTaskService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecurringTemplateResponse>>> GetAll()
    {
        var templates = await _recurringTaskService.GetAllAsync();
        return Ok(templates);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecurringTemplateResponse>> GetById(int id)
    {
        var template = await _recurringTaskService.GetByIdAsync(id);
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<RecurringTemplateResponse>> Create([FromBody] CreateRecurringTemplateRequest request)
    {
        var template = await _recurringTaskService.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RecurringTemplateResponse>> Update(int id, [FromBody] UpdateRecurringTemplateRequest request)
    {
        var template = await _recurringTaskService.UpdateAsync(id, request);
        return Ok(template);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _recurringTaskService.DeactivateAsync(id);
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("ユーザーIDが取得できません");
        return int.Parse(idClaim);
    }
}
