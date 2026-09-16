using BeeCloud.Application.DTOs.NodeMetrics;
using BeeCloud.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BeeCloud.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class NodeMetricsController : ControllerBase
{
    private readonly INodeMetricService _service;

    public NodeMetricsController(
        INodeMetricService service)
    {
        _service = service;
    }

    [HttpGet("node-metrics/{id:guid}")]
    public async Task<ActionResult<NodeMetricResponse>> GetById(
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

    [HttpGet("nodes/{nodeId:guid}/metrics")]
    public async Task<ActionResult<IReadOnlyList<NodeMetricResponse>>> GetByNodeId(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetByNodeIdAsync(
            nodeId,
            cancellationToken);

        return Ok(response);
    }
}