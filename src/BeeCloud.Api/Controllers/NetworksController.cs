using BeeCloud.Application.DTOs.Networks;
using BeeCloud.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BeeCloud.Api.Controllers;

[ApiController]
[Route("api/v1/networks")]
public class NetworksController : ControllerBase
{
    private readonly INetworkService _service;

    public NetworksController(
        INetworkService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<NetworkResponse>> Create(
        [FromBody] CreateNetworkRequest request,
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
    public async Task<ActionResult<NetworkResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NetworkResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var response = await _service.GetAllAsync(
            cancellationToken);

        return Ok(response);
    }
}