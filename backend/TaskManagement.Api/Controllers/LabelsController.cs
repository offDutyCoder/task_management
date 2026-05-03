using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Labels;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/labels")]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly ILabelRequestService _labelRequestService;

    public LabelsController(ILabelService labelService, ILabelRequestService labelRequestService)
    {
        _labelService = labelService;
        _labelRequestService = labelRequestService;
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

    [HttpPost("requests")]
    public async Task<ActionResult<LabelRequestResponse>> CreateRequest([FromBody] CreateLabelRequestDto request)
    {
        var result = await _labelRequestService.CreateAsync(GetCurrentUserId(), request);
        return CreatedAtAction(nameof(GetRequests), result);
    }

    [HttpGet("requests")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<LabelRequestResponse>>> GetRequests([FromQuery] LabelRequestStatus? status)
    {
        var result = await _labelRequestService.GetAllAsync(status);
        return Ok(result);
    }

    [HttpPut("requests/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LabelRequestResponse>> UpdateRequestStatus(int id, [FromBody] UpdateLabelRequestStatusDto dto)
    {
        LabelRequestResponse result;
        if (dto.Status == LabelRequestStatus.Approved)
        {
            if (string.IsNullOrWhiteSpace(dto.Color))
                return BadRequest(new { error = "承認時はカラーコードが必須です" });
            result = await _labelRequestService.ApproveAsync(id, dto.Color);
        }
        else if (dto.Status == LabelRequestStatus.Rejected)
        {
            result = await _labelRequestService.RejectAsync(id);
        }
        else
        {
            return BadRequest(new { error = "statusはApprovedまたはRejectedのみ指定できます" });
        }
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("ユーザーIDが取得できません");
        return int.Parse(idClaim);
    }
}
