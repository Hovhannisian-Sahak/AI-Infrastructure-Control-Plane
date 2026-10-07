using BeeCloud.Application.Interfaces;
using BeeCloud.Application.DTOs.Health;
using BeeCloud.Application.Pagination;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using Moq;

namespace BeeCloud.UnitTests.Application;

[TestFixture]
public class HealthCheckServiceTests
{
    private Mock<IComputeNodeRepository> _nodeRepository = null!;
    private Mock<IHealthCheckRepository> _healthCheckRepository = null!;
    private HealthCheckService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _nodeRepository = new Mock<IComputeNodeRepository>();
        _healthCheckRepository = new Mock<IHealthCheckRepository>();
        _service = new HealthCheckService(
            _nodeRepository.Object,
            _healthCheckRepository.Object);
    }

    [Test]
    public async Task CreateAsync_WithHealthyCheck_PersistsAndMapsHealthCheck()
    {
        var node = CreateNode(NodeStatus.Available);
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _healthCheckRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<HealthCheck>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _healthCheckRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _nodeRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _service.CreateAsync(
            node.Id,
            new HealthCheckRequest
            {
                IsHealthy = true,
                CpuUsagePercent = 45.5,
                GpuUsagePercent = 72.25,
                GpuTemperatureCelsius = 68
            });

        Assert.That(response.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(response.ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(response.IsHealthy, Is.True);
        Assert.That(response.CpuUsagePercent, Is.EqualTo(45.5));
        Assert.That(response.GpuUsagePercent, Is.EqualTo(72.25));
        Assert.That(response.GpuTemperatureCelsius, Is.EqualTo(68));
        Assert.That(response.CheckedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(node.Status, Is.EqualTo(NodeStatus.Available));
        Assert.That(node.LastHealthCheck, Is.EqualTo(response.CheckedAt));

        _healthCheckRepository.Verify(repository =>
            repository.AddAsync(
                It.Is<HealthCheck>(check =>
                    check.ComputeNodeId == node.Id &&
                    check.IsHealthy &&
                    check.CpuUsagePercent == 45.5 &&
                    check.GpuUsagePercent == 72.25),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _healthCheckRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _nodeRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_WithUnhealthyCheck_MarksNodeUnhealthy()
    {
        var node = CreateNode(NodeStatus.Running);
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _healthCheckRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<HealthCheck>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _healthCheckRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _nodeRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _service.CreateAsync(
            node.Id,
            new HealthCheckRequest
            {
                IsHealthy = false,
                CpuUsagePercent = 95
            });

        Assert.That(response.IsHealthy, Is.False);
        Assert.That(node.Status, Is.EqualTo(NodeStatus.Unhealthy));
        Assert.That(node.LastHealthCheck, Is.EqualTo(response.CheckedAt));
    }

    [Test]
    public async Task GetLatestAsync_WhenNoCheckExists_ReturnsNull()
    {
        var node = CreateNode(NodeStatus.Available);
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _healthCheckRepository
            .Setup(repository => repository.GetLatestAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HealthCheck?)null);

        var result = await _service.GetLatestAsync(node.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetHistoryAsync_PassesBoundsAndLimitAndMapsResults()
    {
        var node = CreateNode(NodeStatus.Available);
        var from = DateTime.UtcNow.AddHours(-2);
        var to = DateTime.UtcNow;
        var check = new HealthCheck(node.Id, true, 15, 25, 55);
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _healthCheckRepository
            .Setup(repository => repository.GetHistoryAsync(
                node.Id,
                from,
                to,
                15,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { check });

        var result = await _service.GetHistoryAsync(
            node.Id,
            from,
            to,
            15);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(check.Id));
        Assert.That(result[0].ComputeNodeId, Is.EqualTo(node.Id));
        Assert.That(result[0].CheckedAt, Is.EqualTo(check.CheckedAt));
        _healthCheckRepository.Verify(repository =>
            repository.GetHistoryAsync(
                node.Id,
                from,
                to,
                15,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetHistoryPageAsync_ReturnsCursorsForOlderAndNewerPages()
    {
        var node = CreateNode(NodeStatus.Available);
        var newest = new HealthCheck(node.Id, true, 20, 30, 60);
        await Task.Delay(2);
        var middle = new HealthCheck(node.Id, true, 25, 35, 62);
        await Task.Delay(2);
        var oldest = new HealthCheck(node.Id, true, 30, 40, 64);
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _healthCheckRepository
            .Setup(repository => repository.GetHistoryPageAsync(
                node.Id,
                null,
                null,
                null,
                false,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { newest, middle });

        var firstPage = await _service.GetHistoryPageAsync(
            node.Id,
            null,
            null,
            null,
            previous: false,
            limit: 1);

        Assert.That(firstPage.Items.Select(item => item.Id), Is.EqualTo(new[] { newest.Id }));
        Assert.That(firstPage.PreviousCursor, Is.Null);
        Assert.That(
            HistoryCursorCodec.Decode(firstPage.NextCursor!).Id,
            Is.EqualTo(newest.Id));

        var olderPosition = new HistoryCursor(newest.CheckedAt, newest.Id);
        _healthCheckRepository
            .Setup(repository => repository.GetHistoryPageAsync(
                node.Id,
                null,
                null,
                olderPosition,
                false,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { middle, oldest });

        var olderPage = await _service.GetHistoryPageAsync(
            node.Id,
            null,
            null,
            HistoryCursorCodec.Encode(newest.CheckedAt, newest.Id),
            previous: false,
            limit: 1);

        Assert.That(olderPage.Items.Select(item => item.Id), Is.EqualTo(new[] { middle.Id }));
        Assert.That(olderPage.PreviousCursor, Is.Not.Null);
        Assert.That(olderPage.NextCursor, Is.Not.Null);

        var newerPosition = new HistoryCursor(middle.CheckedAt, middle.Id);
        _healthCheckRepository
            .Setup(repository => repository.GetHistoryPageAsync(
                node.Id,
                null,
                null,
                newerPosition,
                true,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { newest });

        var newerPage = await _service.GetHistoryPageAsync(
            node.Id,
            null,
            null,
            olderPage.PreviousCursor,
            previous: true,
            limit: 1);

        Assert.That(newerPage.Items.Select(item => item.Id), Is.EqualTo(new[] { newest.Id }));
    }

    [Test]
    public void GetHistoryPageAsync_WhenNodeDoesNotExist_ThrowsNotFound()
    {
        var nodeId = Guid.NewGuid();
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                nodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComputeNode?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.GetHistoryPageAsync(
                nodeId,
                null,
                null,
                null,
                previous: false,
                limit: 25));
    }

    [Test]
    public void GetHistoryPageAsync_WhenCursorIsMalformed_ThrowsArgumentException()
    {
        var node = CreateNode(NodeStatus.Available);
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetHistoryPageAsync(
                node.Id,
                null,
                null,
                "not-a-valid-cursor",
                previous: false,
                limit: 25));
    }

    [Test]
    public void GetHistoryAsync_WhenLimitIsOutsideBounds_ThrowsArgumentException()
    {
        var node = CreateNode(NodeStatus.Available);
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetHistoryAsync(
                node.Id,
                limit: 0));
    }

    [Test]
    public void GetHistoryAsync_WhenTimeRangeIsReversed_ThrowsArgumentException()
    {
        var node = CreateNode(NodeStatus.Available);
        _nodeRepository
            .Setup(repository => repository.GetByIdAsync(
                node.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetHistoryAsync(
                node.Id,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(-1)));
    }

    private static ComputeNode CreateNode(NodeStatus status)
    {
        var node = new ComputeNode(
            $"health-service-test-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);

        if (status is NodeStatus.Available or NodeStatus.Running)
        {
            node.MarkAvailable();
        }

        if (status == NodeStatus.Running)
        {
            node.Start();
        }

        return node;
    }
}
