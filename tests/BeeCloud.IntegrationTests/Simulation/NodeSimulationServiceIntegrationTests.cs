using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.IntegrationTests.Simulation;

[TestFixture]
public class NodeSimulationServiceIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private NodeSimulationService _service = null!;

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

        _service =
            new NodeSimulationService(repository);
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
    [TestCase(NodeFault.GpuFailure)]
    [TestCase(NodeFault.GpuOverheat)]
    [TestCase(NodeFault.NetworkFailure)]
    [TestCase(NodeFault.ServiceCrash)]
    public async Task SimulateFaultAsync_WhenValidFaultIsSimulated_ShouldPersistFault(
        NodeFault fault)
    {
        // Arrange
        var node = new ComputeNode(
            "integration-test-node",
            "NVIDIA A100",
            2);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        // Act
        var response =
            await _service.SimulateFaultAsync(
                node.Id,
                fault);

        // Assert - service response
        Assert.That(
            response.Id,
            Is.EqualTo(node.Id));

        Assert.That(
            response.ActiveFault,
            Is.EqualTo(fault.ToString()));

        // Assert - database
        _dbContext.ChangeTracker.Clear();

        var persistedNode =
            await _dbContext.ComputeNodes
                .SingleAsync(x => x.Id == node.Id);

        Assert.That(
            persistedNode.ActiveFault,
            Is.EqualTo(fault));
    }
    [Test]
    public async Task SimulateFaultAsync_WhenNoneIsProvided_ShouldThrowArgumentException()
    {
        // Arrange
        var node = new ComputeNode(
            "integration-test-node",
            "NVIDIA A100",
            2);

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception =
            Assert.ThrowsAsync<ArgumentException>(
                async () =>
                    await _service.SimulateFaultAsync(
                        node.Id,
                        NodeFault.None));

        Assert.That(
            exception!.Message,
            Is.EqualTo("Fault must be specified."));
    }
}