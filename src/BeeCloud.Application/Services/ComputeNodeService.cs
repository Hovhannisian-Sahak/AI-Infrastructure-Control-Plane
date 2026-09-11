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
            throw new ArgumentException(
                "GPU count must be greater than zero.");
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

    public async Task<ComputeNodeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (node is null)
        {
            return null;
        }

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
    public async Task<IReadOnlyList<ComputeNodeResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var nodes = await _repository.GetAllAsync(
            cancellationToken);

        return nodes
            .Select(node => new ComputeNodeResponse
            {
                Id = node.Id,
                Name = node.Name,
                GpuModel = node.GpuModel,
                GpuCount = node.GpuCount,
                Status = node.Status.ToString(),
                CreatedAt = node.CreatedAt,
                UpdatedAt = node.UpdatedAt,
                LastHealthCheck = node.LastHealthCheck
            })
            .ToList();
    }
    public async Task<ComputeNodeResponse> StartAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{id}' was not found.");
        }

        node.Start();

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
    public async Task<ComputeNodeResponse> StopAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var node = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{id}' was not found.");
        }

        node.Stop();

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