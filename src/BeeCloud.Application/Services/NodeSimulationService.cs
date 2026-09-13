using BeeCloud.Application.DTOs.ComputeNodes;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Enums;

namespace BeeCloud.Application.Services;

public class NodeSimulationService : INodeSimulationService
{
    private readonly IComputeNodeRepository _repository;

    public NodeSimulationService(
        IComputeNodeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ComputeNodeResponse> SimulateFaultAsync(
        Guid nodeId,
        NodeFault fault,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetByIdAsync(
            nodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{nodeId}' was not found.");
        }

        if (fault == NodeFault.None)
        {
            throw new ArgumentException(
                "Fault must be specified.");
        }

        node.SimulateFault(fault);

        await _repository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(node);
    }

    public async Task<ComputeNodeResponse> ClearFaultAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetByIdAsync(
            nodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{nodeId}' was not found.");
        }

        node.ClearFault();

        await _repository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(node);
    }

    private static ComputeNodeResponse MapToResponse(
        BeeCloud.Domain.Entities.ComputeNode node)
    {
        return new ComputeNodeResponse
        {
            Id = node.Id,
            Name = node.Name,
            GpuModel = node.GpuModel,
            GpuCount = node.GpuCount,
            Status = node.Status.ToString(),
            ActiveFault = node.ActiveFault.ToString(),
            CreatedAt = node.CreatedAt,
            UpdatedAt = node.UpdatedAt,
            LastHealthCheck = node.LastHealthCheck
        };
    }
}