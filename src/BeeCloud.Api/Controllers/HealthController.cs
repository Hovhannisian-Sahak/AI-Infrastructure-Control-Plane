using BeeCloud.Application.DTOs.Health;
using BeeCloud.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BeeCloud.Api.Controllers;

[ApiController]
[Route("api/v1/nodes/{nodeId:guid}/health")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _service;

    public HealthController(IHealthCheckService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<HealthCheckResponse>> Create(
        Guid nodeId,
        [FromBody] HealthCheckRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(
            nodeId,
            request,
            cancellationToken);

        // return Created(
        //     $"/api/v1/nodes/{nodeId}/health/{response.Id}",
        //     response);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<HealthCheckResponse>> GetLatest(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetLatestAsync(
            nodeId,
            cancellationToken);

        if (response is null)
        {
            return NoContent();
        }

        return Ok(response);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<HealthCheckResponse>>>
        GetHistory(
            Guid nodeId,
            CancellationToken cancellationToken)
    {
        var response = await _service.GetHistoryAsync(
            nodeId,
            cancellationToken);

        return Ok(response);
    }
}