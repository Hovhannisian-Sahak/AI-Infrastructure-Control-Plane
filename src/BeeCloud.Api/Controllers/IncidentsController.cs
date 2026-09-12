using BeeCloud.Application.DTOs.Incidents;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BeeCloud.Api.Controllers;

[ApiController]
[Route("api/v1/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _service;

    public IncidentsController(IIncidentService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<IncidentResponse>> Create(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (response is null)
            return NotFound();

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IncidentResponse>>> GetAll(
        [FromQuery] Guid? computeNodeId,
        [FromQuery] IncidentSeverity? severity,
        [FromQuery] IncidentStatus? status,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetAllAsync(
            computeNodeId,
            severity,
            status,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/investigate")]
    public async Task<ActionResult<IncidentResponse>> StartInvestigation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _service.StartInvestigationAsync(
            id,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<IncidentResponse>> Resolve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _service.ResolveAsync(
            id,
            cancellationToken);

        return Ok(response);
    }
}