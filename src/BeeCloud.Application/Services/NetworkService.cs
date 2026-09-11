using BeeCloud.Application.DTOs.Networks;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Services;

public class NetworkService : INetworkService
{
    private readonly INetworkRepository _repository;

    public NetworkService(
        INetworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<NetworkResponse> CreateAsync(
        CreateNetworkRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Network name is required.");
        }

        var nameExists = await _repository.ExistsByNameAsync(
            request.Name,
            cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException(
                $"A network with name '{request.Name}' already exists.");
        }

        var network = new Network(
            request.Name,
            request.Description);

        await _repository.AddAsync(
            network,
            cancellationToken);

        await _repository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(network);
    }

    public async Task<NetworkResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var network = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (network is null)
        {
            return null;
        }

        return MapToResponse(network);
    }

    public async Task<IReadOnlyList<NetworkResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var networks = await _repository.GetAllAsync(
            cancellationToken);

        return networks
            .Select(MapToResponse)
            .ToList();
    }

    private static NetworkResponse MapToResponse(
        Network network)
    {
        return new NetworkResponse
        {
            Id = network.Id,
            Name = network.Name,
            Description = network.Description,
            CreatedAt = network.CreatedAt
        };
    }
}