using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Labels;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/labels")]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labelService;

    public LabelsController(ILabelService labelService)
    {
        _labelService = labelService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LabelResponse>>> GetAll()
    {
        var labels = await _labelService.GetAllAsync();
        return Ok(labels);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LabelResponse>> Create([FromBody] CreateLabelRequest request)
    {
        var label = await _labelService.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), label);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LabelResponse>> Update(int id, [FromBody] UpdateLabelRequest request)
    {
        var label = await _labelService.UpdateAsync(id, request);
        return Ok(label);
    }
}
