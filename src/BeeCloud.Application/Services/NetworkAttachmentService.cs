using BeeCloud.Application.DTOs.Networks;
using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;

namespace BeeCloud.Application.Services;

public class NetworkAttachmentService
    : INetworkAttachmentService
{
    private readonly IComputeNodeRepository _computeNodeRepository;
    private readonly INetworkRepository _networkRepository;
    private readonly INetworkAttachmentRepository _attachmentRepository;

    public NetworkAttachmentService(
        IComputeNodeRepository computeNodeRepository,
        INetworkRepository networkRepository,
        INetworkAttachmentRepository attachmentRepository)
    {
        _computeNodeRepository = computeNodeRepository;
        _networkRepository = networkRepository;
        _attachmentRepository = attachmentRepository;
    }

    public async Task<NetworkAttachmentResponse> AttachAsync(
        Guid computeNodeId,
        Guid networkId,
        CancellationToken cancellationToken = default)
    {
        var node = await _computeNodeRepository.GetByIdAsync(
            computeNodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{computeNodeId}' was not found.");
        }

        var network = await _networkRepository.GetByIdAsync(
            networkId,
            cancellationToken);

        if (network is null)
        {
            throw new KeyNotFoundException(
                $"Network with id '{networkId}' was not found.");
        }

        var existingAttachment =
            await _attachmentRepository.GetAsync(
                computeNodeId,
                networkId,
                cancellationToken);

        if (existingAttachment is not null)
        {
            throw new InvalidOperationException(
                $"Compute node '{computeNodeId}' is already attached " +
                $"to network '{networkId}'.");
        }

        var attachment = new NetworkAttachment(
            computeNodeId,
            networkId);

        await _attachmentRepository.AddAsync(
            attachment,
            cancellationToken);

        await _attachmentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToResponse(attachment);
    }

    public async Task DetachAsync(
        Guid computeNodeId,
        Guid networkId,
        CancellationToken cancellationToken = default)
    {
        var attachment =
            await _attachmentRepository.GetAsync(
                computeNodeId,
                networkId,
                cancellationToken);

        if (attachment is null)
        {
            throw new KeyNotFoundException(
                $"Compute node '{computeNodeId}' is not attached " +
                $"to network '{networkId}'.");
        }

        _attachmentRepository.Remove(attachment);

        await _attachmentRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkAttachmentResponse>>
        GetByNodeIdAsync(
            Guid computeNodeId,
            CancellationToken cancellationToken = default)
    {
        var node = await _computeNodeRepository.GetByIdAsync(
            computeNodeId,
            cancellationToken);

        if (node is null)
        {
            throw new KeyNotFoundException(
                $"Compute node with id '{computeNodeId}' was not found.");
        }

        var attachments =
            await _attachmentRepository.GetByNodeIdAsync(
                computeNodeId,
                cancellationToken);

        return attachments
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<IReadOnlyList<NetworkAttachmentResponse>>
        GetByNetworkIdAsync(
            Guid networkId,
            CancellationToken cancellationToken = default)
    {
        var network = await _networkRepository.GetByIdAsync(
            networkId,
            cancellationToken);

        if (network is null)
        {
            throw new KeyNotFoundException(
                $"Network with id '{networkId}' was not found.");
        }

        var attachments =
            await _attachmentRepository.GetByNetworkIdAsync(
                networkId,
                cancellationToken);

        return attachments
            .Select(MapToResponse)
            .ToList();
    }

    private static NetworkAttachmentResponse MapToResponse(
        NetworkAttachment attachment)
    {
        return new NetworkAttachmentResponse
        {
            Id = attachment.Id,
            ComputeNodeId = attachment.ComputeNodeId,
            NetworkId = attachment.NetworkId,
            AttachedAt = attachment.AttachedAt
        };
    }
}