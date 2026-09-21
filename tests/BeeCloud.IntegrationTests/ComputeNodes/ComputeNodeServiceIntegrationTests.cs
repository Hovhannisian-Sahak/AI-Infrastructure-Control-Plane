using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Fakes;
using BeeCloud.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.IntegrationTests.ComputeNodes;

[TestFixture]
public class ComputeNodeServiceIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private ComputeNodeService _service = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlTestContainer();

        await _postgres.StartAsync();

        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_postgres.ConnectionString)
                .Options;

        _dbContext =
            new ApplicationDbContext(options);

        await _dbContext.Database.EnsureCreatedAsync();

        var repository =
            new ComputeNodeRepository(_dbContext);

        var provisioningQueue =
            new FakeProvisioningQueue();

        _service =
            new ComputeNodeService(
                repository,
                provisioningQueue);
    }

    [SetUp]
    public async Task SetUp()
    {
        _dbContext.ChangeTracker.Clear();

        await _dbContext.ComputeNodes.ExecuteDeleteAsync();

        _dbContext.ChangeTracker.Clear();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task SimulateFaultAsync_WhenGpuFailureIsSimulated_ShouldPersistFault()
    {
        // Arrange
        var node = new ComputeNode(
            "integration-test-node",
            "NVIDIA A100",
            2);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.SimulateFaultAsync(
            node.Id,
            NodeFault.GpuFailure);

        // Assert
        _dbContext.ChangeTracker.Clear();

        var persistedNode =
            await _dbContext.ComputeNodes
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            persistedNode.ActiveFault,
            Is.EqualTo(NodeFault.GpuFailure));
    }
}