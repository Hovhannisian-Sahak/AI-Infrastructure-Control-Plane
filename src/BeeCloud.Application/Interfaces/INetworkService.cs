using BeeCloud.Application.DTOs.Networks;

namespace BeeCloud.Application.Interfaces;

public interface INetworkService
{
    Task<NetworkResponse> CreateAsync(
        CreateNetworkRequest request,
        CancellationToken cancellationToken = default);

    Task<NetworkResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}