using BeeCloud.Domain.Entities;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.IntegrationTests.Networks;

[TestFixture]
public class NetworkAttachmentRepositoryIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private NetworkAttachmentRepository _repository = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlTestContainer();

        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.ConnectionString)
            .Options;

        _dbContext = new ApplicationDbContext(options);

        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new NetworkAttachmentRepository(_dbContext);
    }

    [SetUp]
    public async Task SetUp()
    {
        await _dbContext.NetworkAttachments.ExecuteDeleteAsync();
        await _dbContext.ComputeNodes.ExecuteDeleteAsync();
        await _dbContext.Networks.ExecuteDeleteAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task AddAsync_ShouldPersistAttachmentToPostgreSql()
    {
        // Arrange
        var node = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        var network = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.Networks.AddAsync(network);

        await _dbContext.SaveChangesAsync();

        var attachment = new NetworkAttachment(
            node.Id,
            network.Id);

        // Act
        await _repository.AddAsync(attachment);
        await _repository.SaveChangesAsync();

        // Assert
        var savedAttachment =
            await _dbContext.NetworkAttachments
                .AsNoTracking()
                .SingleAsync(x => x.Id == attachment.Id);

        Assert.Multiple(() =>
        {
            Assert.That(
                savedAttachment.ComputeNodeId,
                Is.EqualTo(node.Id));

            Assert.That(
                savedAttachment.NetworkId,
                Is.EqualTo(network.Id));

            Assert.That(
                savedAttachment.Id,
                Is.EqualTo(attachment.Id));
        });
    }

    [Test]
    public async Task GetAsync_WhenAttachmentExists_ShouldReturnAttachment()
    {
        // Arrange
        var node = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        var network = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.Networks.AddAsync(network);

        var attachment = new NetworkAttachment(
            node.Id,
            network.Id);

        await _dbContext.NetworkAttachments.AddAsync(attachment);

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(
            node.Id,
            network.Id);

        // Assert
        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(attachment.Id));
            Assert.That(
                result.ComputeNodeId,
                Is.EqualTo(node.Id));
            Assert.That(
                result.NetworkId,
                Is.EqualTo(network.Id));
        });
    }

    [Test]
    public async Task GetAsync_WhenAttachmentDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetAsync(
            Guid.NewGuid(),
            Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByNodeIdAsync_ShouldReturnAllNodeAttachments()
    {
        // Arrange
        var node = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        var network1 = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        var network2 = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        await _dbContext.ComputeNodes.AddAsync(node);

        await _dbContext.Networks.AddRangeAsync(
            network1,
            network2);

        var attachment1 = new NetworkAttachment(
            node.Id,
            network1.Id);

        var attachment2 = new NetworkAttachment(
            node.Id,
            network2.Id);

        await _dbContext.NetworkAttachments.AddRangeAsync(
            attachment1,
            attachment2);

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNodeIdAsync(node.Id);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        Assert.That(
            result.Select(x => x.Id),
            Is.EquivalentTo(new[]
            {
                attachment1.Id,
                attachment2.Id
            }));
    }

    [Test]
    public async Task GetByNetworkIdAsync_ShouldReturnAllNetworkAttachments()
    {
        // Arrange
        var node1 = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        var node2 = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA H100",
            2);

        var network = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        await _dbContext.ComputeNodes.AddRangeAsync(
            node1,
            node2);

        await _dbContext.Networks.AddAsync(network);

        var attachment1 = new NetworkAttachment(
            node1.Id,
            network.Id);

        var attachment2 = new NetworkAttachment(
            node2.Id,
            network.Id);

        await _dbContext.NetworkAttachments.AddRangeAsync(
            attachment1,
            attachment2);

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNetworkIdAsync(network.Id);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        Assert.That(
            result.Select(x => x.Id),
            Is.EquivalentTo(new[]
            {
                attachment1.Id,
                attachment2.Id
            }));
    }

    [Test]
    public async Task Remove_ShouldDeleteAttachmentFromPostgreSql()
    {
        // Arrange
        var node = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        var network = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        var attachment = new NetworkAttachment(
            node.Id,
            network.Id);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.Networks.AddAsync(network);
        await _dbContext.NetworkAttachments.AddAsync(attachment);

        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(attachment);
        await _repository.SaveChangesAsync();

        // Assert
        var exists = await _dbContext.NetworkAttachments
            .AnyAsync(x => x.Id == attachment.Id);

        Assert.That(exists, Is.False);
    }
    [Test]
    public async Task AddAsync_WhenDuplicateAttachmentExists_ShouldFail()
    {
        // Arrange
        var node = new ComputeNode(
            $"integration-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            4);

        var network = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.Networks.AddAsync(network);

        await _dbContext.SaveChangesAsync();

        var firstAttachment = new NetworkAttachment(
            node.Id,
            network.Id);

        var secondAttachment = new NetworkAttachment(
            node.Id,
            network.Id);

        await _repository.AddAsync(firstAttachment);
        await _repository.SaveChangesAsync();

        // Act
        await _repository.AddAsync(secondAttachment);

        // Assert
        Assert.ThrowsAsync<DbUpdateException>(
            async () =>
                await _repository.SaveChangesAsync());
    }
}