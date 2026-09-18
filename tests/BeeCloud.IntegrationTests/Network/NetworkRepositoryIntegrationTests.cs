using BeeCloud.Domain.Entities;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BeeCloud.IntegrationTests.Networks;

[TestFixture]
public class NetworkRepositoryIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private NetworkRepository _repository = null!;

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

        _repository = new NetworkRepository(_dbContext);
    }

    [SetUp]
    public async Task SetUp()
    {
        await _dbContext.Networks.ExecuteDeleteAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task AddAsync_ShouldPersistNetworkToPostgreSql()
    {
        // Arrange
        var network = new Network(
            $"integration-network-{Guid.NewGuid():N}",
            "Integration test network");

        // Act
        await _repository.AddAsync(network);
        await _repository.SaveChangesAsync();

        // Assert
        var savedNetwork = await _dbContext.Networks
            .AsNoTracking()
            .SingleAsync(x => x.Id == network.Id);

        Assert.Multiple(() =>
        {
            Assert.That(savedNetwork.Id, Is.EqualTo(network.Id));
            Assert.That(savedNetwork.Name, Is.EqualTo(network.Name));
            Assert.That(
                savedNetwork.Description,
                Is.EqualTo(network.Description));
        });
    }

    [Test]
    public async Task GetByIdAsync_WhenNetworkExists_ShouldReturnNetwork()
    {
        // Arrange
        var network = new Network(
            $"integration-network-{Guid.NewGuid():N}",
            "Test network");

        await _dbContext.Networks.AddAsync(network);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(network.Id);

        // Assert
        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(network.Id));
            Assert.That(result.Name, Is.EqualTo(network.Name));
            Assert.That(result.Description, Is.EqualTo(network.Description));
        });
    }

    [Test]
    public async Task GetByIdAsync_WhenNetworkDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnPersistedNetworks()
    {
        // Arrange
        var network1 = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        var network2 = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        await _dbContext.Networks.AddRangeAsync(
            network1,
            network2);

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        Assert.That(
            result.Select(x => x.Id),
            Is.EquivalentTo(new[]
            {
                network1.Id,
                network2.Id
            }));
    }

    [Test]
    public async Task ExistsByNameAsync_WhenNetworkExists_ShouldReturnTrue()
    {
        // Arrange
        var network = new Network(
            $"integration-network-{Guid.NewGuid():N}");

        await _dbContext.Networks.AddAsync(network);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByNameAsync(network.Name);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExistsByNameAsync_WhenNetworkDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsByNameAsync(
            $"missing-network-{Guid.NewGuid():N}");

        // Assert
        Assert.That(result, Is.False);
    }
}