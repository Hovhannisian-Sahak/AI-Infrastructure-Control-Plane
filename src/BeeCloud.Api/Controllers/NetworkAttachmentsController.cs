using BeeCloud.Application.DTOs.Networks;
using BeeCloud.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BeeCloud.Api.Controllers;

[ApiController]
[Route("api/v1/nodes/{nodeId:guid}/networks")]
public class NetworkAttachmentsController : ControllerBase
{
    private readonly INetworkAttachmentService _service;

    public NetworkAttachmentsController(
        INetworkAttachmentService service)
    {
        _service = service;
    }

    [HttpPost("{networkId:guid}")]
    public async Task<IActionResult> Attach(
        Guid nodeId,
        Guid networkId,
        CancellationToken cancellationToken)
    {
        var attachment = await _service.AttachAsync(
            nodeId,
            networkId,
            cancellationToken);

        return Created(
            $"/api/v1/nodes/{nodeId}/networks/{networkId}",
            new
            {
                attachment.Id,
                attachment.ComputeNodeId,
                attachment.NetworkId,
                attachment.AttachedAt
            });
    }

    [HttpDelete("{networkId:guid}")]
    public async Task<IActionResult> Detach(
        Guid nodeId,
        Guid networkId,
        CancellationToken cancellationToken)
    {
        await _service.DetachAsync(
            nodeId,
            networkId,
            cancellationToken);

        return NoContent();
    }
    
    [HttpGet("/api/v1/nodes/{nodeId:guid}/networks")]
    public async Task<ActionResult<IReadOnlyList<NetworkAttachmentResponse>>>
        GetNodeNetworks(
            Guid nodeId,
            CancellationToken cancellationToken)
    {
        var response = await _service.GetByNodeIdAsync(
            nodeId,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("/api/v1/networks/{networkId:guid}/nodes")]
    public async Task<ActionResult<IReadOnlyList<NetworkAttachmentResponse>>>
        GetNetworkNodes(
            Guid networkId,
            CancellationToken cancellationToken)
    {
        var response = await _service.GetByNetworkIdAsync(
            networkId,
            cancellationToken);

        return Ok(response);
    }
}