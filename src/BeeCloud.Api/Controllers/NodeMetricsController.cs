using BeeCloud.Application.DTOs.NodeMetrics;
using BeeCloud.Application.DTOs.Pagination;
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
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var response = await _service.GetHistoryAsync(
            nodeId,
            from,
            to,
            limit,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("nodes/{nodeId:guid}/metrics/page")]
    public async Task<ActionResult<CursorPageResponse<NodeMetricResponse>>> GetHistoryPage(
        Guid nodeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? cursor,
        [FromQuery] bool previous = false,
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        var response = await _service.GetHistoryPageAsync(
            nodeId, from, to, cursor, previous, limit, cancellationToken);
        return Ok(response);
    }
}