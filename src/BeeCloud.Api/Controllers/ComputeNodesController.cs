using BeeCloud.Application.DTOs.ComputeNodes;
using BeeCloud.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BeeCloud.Api.Controllers;

[ApiController]
[Route("api/v1/nodes")]
public class ComputeNodesController : ControllerBase
{
    private readonly IComputeNodeService _service;

    public ComputeNodesController(IComputeNodeService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ComputeNodeResponse>> Create(
        [FromBody] CreateComputeNodeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(
            request,
            cancellationToken);

        return Accepted(response);
    }
}