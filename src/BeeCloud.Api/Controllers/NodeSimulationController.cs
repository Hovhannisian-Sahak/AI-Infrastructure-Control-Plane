using BeeCloud.Application.DTOs.ComputeNodes;
using BeeCloud.Application.DTOs.Simulation;
using BeeCloud.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BeeCloud.Api.Controllers;

[ApiController]
[Route("api/v1/nodes/{nodeId:guid}/simulate")]
public class NodeSimulationController : ControllerBase
{
    private readonly INodeSimulationService _service;

    public NodeSimulationController(
        INodeSimulationService service)
    {
        _service = service;
    }

    [HttpPost("fault")]
    public async Task<ActionResult<ComputeNodeResponse>> SimulateFault(
        Guid nodeId,
        [FromBody] SimulateFaultRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _service.SimulateFaultAsync(
            nodeId,
            request.Fault,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("fault")]
    public async Task<ActionResult<ComputeNodeResponse>> ClearFault(
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var response = await _service.ClearFaultAsync(
            nodeId,
            cancellationToken);

        return Ok(response);
    }
}