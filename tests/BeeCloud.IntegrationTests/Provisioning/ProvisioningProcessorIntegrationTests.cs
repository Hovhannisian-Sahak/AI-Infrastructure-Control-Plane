using BeeCloud.Application.Interfaces;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using BeeCloud.Worker.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace BeeCloud.IntegrationTests.Provisioning;

[TestFixture]
public class ProvisioningProcessorIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private ProvisioningProcessor _processor = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlTestContainer();

        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.ConnectionString)
            .Options;

        _dbContext = new ApplicationDbContext(options);

        await _dbContext.Database.MigrateAsync();

        IComputeNodeRepository nodeRepository =
            new ComputeNodeRepository(_dbContext);

        _processor = new ProvisioningProcessor(
            nodeRepository,
            NullLogger<ProvisioningProcessor>.Instance);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task ProcessAsync_WithProvisioningNode_ShouldMakeNodeAvailable()
    {
        var node = await CreateProvisioningNodeAsync();

        await _processor.ProcessAsync();

        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Available));
    }

    [Test]
    public async Task ProcessAsync_WithMultipleProvisioningNodes_ShouldMakeAllAvailable()
    {
        var firstNode = await CreateProvisioningNodeAsync();
        var secondNode = await CreateProvisioningNodeAsync();

        await _processor.ProcessAsync();

        var persistedNodes = await _dbContext.ComputeNodes
            .AsNoTracking()
            .Where(n => n.Id == firstNode.Id || n.Id == secondNode.Id)
            .ToListAsync();

        Assert.That(persistedNodes, Has.Count.EqualTo(2));

        Assert.That(
            persistedNodes.All(n => n.Status == NodeStatus.Available),
            Is.True);
    }

    [Test]
    public async Task ProcessAsync_WithNonProvisioningNode_ShouldNotChangeStatus()
    {
        var node = new ComputeNode(
            $"provisioning-test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);

        node.MarkAvailable();

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        await _processor.ProcessAsync();

        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Available));
    }

    [Test]
    public async Task ProcessAsync_WithNoProvisioningNodes_ShouldCompleteSuccessfully()
    {
        Assert.DoesNotThrowAsync(
            async () => await _processor.ProcessAsync());
    }

    private async Task<ComputeNode> CreateProvisioningNodeAsync()
    {
        var node = new ComputeNode(
            $"provisioning-test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        return node;
    }
}