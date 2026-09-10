using BeeCloud.Application.DTOs.ComputeNodes;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Services;

public class ComputeNodeService : IComputeNodeService
{
    private readonly IComputeNodeRepository _repository;

    public ComputeNodeService(IComputeNodeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ComputeNodeResponse> CreateAsync(
        CreateComputeNodeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Node name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.GpuModel))
        {
            throw new ArgumentException("GPU model is required.");
        }

        if (request.GpuCount <= 0)
        {
            throw new ArgumentException("GPU count must be greater than zero.");
        }

        var nameExists = await _repository.ExistsByNameAsync(
            request.Name,
            cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException(
                $"A compute node with name '{request.Name}' already exists.");
        }

        var node = new ComputeNode(
            request.Name,
            request.GpuModel,
            request.GpuCount);

        await _repository.AddAsync(
            node,
            cancellationToken);

        await _repository.SaveChangesAsync(
            cancellationToken);

        return new ComputeNodeResponse
        {
            Id = node.Id,
            Name = node.Name,
            GpuModel = node.GpuModel,
            GpuCount = node.GpuCount,
            Status = node.Status.ToString(),
            CreatedAt = node.CreatedAt,
            UpdatedAt = node.UpdatedAt,
            LastHealthCheck = node.LastHealthCheck
        };
    }
}