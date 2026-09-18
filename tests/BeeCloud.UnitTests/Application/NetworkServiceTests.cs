using BeeCloud.Application.DTOs.Networks;
using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using Moq;

namespace BeeCloud.UnitTests.Application;

[TestFixture]
public class NetworkServiceTests
{
    private Mock<INetworkRepository> _repositoryMock = null!;
    private NetworkService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<INetworkRepository>();
        _service = new NetworkService(_repositoryMock.Object);
    }

    [Test]
    public async Task CreateAsync_WithValidRequest_ShouldCreateAndSaveNetwork()
    {
        // Arrange
        var request = new CreateNetworkRequest
        {
            Name = "production-network",
            Description = "Production GPU network"
        };

        _repositoryMock
            .Setup(x => x.ExistsByNameAsync(
                request.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Name, Is.EqualTo(request.Name));
            Assert.That(result.Description, Is.EqualTo(request.Description));
        });

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.Is<Network>(n =>
                    n.Name == request.Name &&
                    n.Description == request.Description),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void CreateAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateNetworkRequest
        {
            Name = "",
            Description = "Test network"
        };

        // Act
        var exception = Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(request));

        // Assert
        Assert.That(exception!.Message, Does.Contain("Network name is required"));

        _repositoryMock.Verify(
            x => x.ExistsByNameAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Network>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void CreateAsync_WithDuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new CreateNetworkRequest
        {
            Name = "production-network",
            Description = "Duplicate network"
        };

        _repositoryMock
            .Setup(x => x.ExistsByNameAsync(
                request.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.CreateAsync(request));

        // Assert
        Assert.That(
            exception!.Message,
            Does.Contain("A network with name 'production-network' already exists."));

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Network>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_WhenNetworkExists_ShouldReturnNetworkResponse()
    {
        // Arrange
        var network = new Network(
            "production-network",
            "Production GPU network");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                network.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(network);

        // Act
        var result = await _service.GetByIdAsync(network.Id);

        // Assert
        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(network.Id));
            Assert.That(result.Name, Is.EqualTo(network.Name));
            Assert.That(result.Description, Is.EqualTo(network.Description));
            Assert.That(result.CreatedAt, Is.EqualTo(network.CreatedAt));
        });
    }

    [Test]
    public async Task GetByIdAsync_WhenNetworkDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var networkId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                networkId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Network?)null);

        // Act
        var result = await _service.GetByIdAsync(networkId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnMappedNetworkResponses()
    {
        // Arrange
        var firstNetwork = new Network(
            "network-1",
            "First network");

        var secondNetwork = new Network(
            "network-2",
            "Second network");

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Network>
            {
                firstNetwork,
                secondNetwork
            });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo(firstNetwork.Id));
            Assert.That(result[0].Name, Is.EqualTo(firstNetwork.Name));
            Assert.That(result[0].Description, Is.EqualTo(firstNetwork.Description));

            Assert.That(result[1].Id, Is.EqualTo(secondNetwork.Id));
            Assert.That(result[1].Name, Is.EqualTo(secondNetwork.Name));
            Assert.That(result[1].Description, Is.EqualTo(secondNetwork.Description));
        });
    }

    [Test]
    public async Task CreateAsync_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        var request = new CreateNetworkRequest
        {
            Name = "production-network",
            Description = "Production network"
        };

        _repositoryMock
            .Setup(x => x.ExistsByNameAsync(
                request.Name,
                cancellationToken))
            .ReturnsAsync(false);

        // Act
        await _service.CreateAsync(request, cancellationToken);

        // Assert
        _repositoryMock.Verify(
            x => x.ExistsByNameAsync(
                request.Name,
                cancellationToken),
            Times.Once);

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Network>(),
                cancellationToken),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(cancellationToken),
            Times.Once);
    }
}