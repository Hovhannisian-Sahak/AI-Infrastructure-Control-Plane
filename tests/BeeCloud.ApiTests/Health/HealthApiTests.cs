using System.Net;
using BeeCloud.ApiTests;
using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.TestData;
using BeeCloud.Application.DTOs.Health;

namespace BeeCloud.ApiTests.Health;

[TestFixture]
public class HealthApiTests
{
    private NodesClient _nodesClient = null!;
    private HealthClient _healthClient = null!;
    private ApiTestDataHelper _testData = null!;

    [SetUp]
    public void SetUp()
    {
        var baseUrl = TestConfiguration.BaseUrl;

        _nodesClient = new NodesClient(baseUrl);
        _healthClient = new HealthClient(baseUrl);
        _testData = new ApiTestDataHelper(_nodesClient);
    }

    [Test]
    public async Task CreateHealthCheck_ThenGetLatest_ShouldReturnRecordedCheck()
    {
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var request = new HealthCheckRequest
        {
            IsHealthy = true,
            CpuUsagePercent = 42.5,
            GpuUsagePercent = 76.25,
            GpuTemperatureCelsius = 71.5
        };

        var createResponse = await _healthClient.CreateAsync(nodeId, request);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(createResponse.Data, Is.Not.Null);
        Assert.That(createResponse.Data!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(createResponse.Data.ComputeNodeId, Is.EqualTo(nodeId));
        Assert.That(createResponse.Data.IsHealthy, Is.True);
        Assert.That(createResponse.Data.CpuUsagePercent, Is.EqualTo(request.CpuUsagePercent));
        Assert.That(createResponse.Data.GpuUsagePercent, Is.EqualTo(request.GpuUsagePercent));
        Assert.That(
            createResponse.Data.GpuTemperatureCelsius,
            Is.EqualTo(request.GpuTemperatureCelsius));
        Assert.That(createResponse.Data.CheckedAt, Is.Not.EqualTo(default(DateTime)));

        var latestResponse = await _healthClient.GetLatestAsync(nodeId);

        Assert.That(latestResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(latestResponse.Data, Is.Not.Null);
        Assert.That(latestResponse.Data!.Id, Is.EqualTo(createResponse.Data.Id));
    }

    [Test]
    public async Task GetLatest_WhenNodeDoesNotExist_ShouldReturnNotFound()
    {
        var response = await _healthClient.GetLatestAsync(Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetHistory_ShouldApplyTimeBoundsAndLimit()
    {
        var nodeId = await _testData.CreateAvailableNodeAsync();
        for (var index = 0; index < 3; index++)
        {
            var createResponse = await _healthClient.CreateAsync(
                nodeId,
                new HealthCheckRequest
                {
                    IsHealthy = true,
                    CpuUsagePercent = 20 + index
                });

            Assert.That(
                createResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                $"Health check creation failed. Response: {createResponse.Content}");
            await Task.Delay(15);
        }

        var response = await _healthClient.GetHistoryAsync(
            nodeId,
            from: DateTime.UtcNow.AddMinutes(-1),
            to: DateTime.UtcNow.AddMinutes(1),
            limit: 2);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Null);
        Assert.That(response.Data, Has.Count.EqualTo(2));
        Assert.That(
            response.Data!,
            Has.All.Matches<HealthCheckResponse>(
                item => item.ComputeNodeId == nodeId && item.IsHealthy));
    }

    [Test]
    public async Task GetHistoryPage_ShouldNavigateOlderAndNewerPages()
    {
        var nodeId = await _testData.CreateAvailableNodeAsync();
        for (var index = 0; index < 3; index++)
        {
            var createResponse = await _healthClient.CreateAsync(
                nodeId,
                new HealthCheckRequest
                {
                    IsHealthy = true,
                    CpuUsagePercent = 30 + index
                });

            Assert.That(
                createResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                $"Health check creation failed. Response: {createResponse.Content}");
            await Task.Delay(15);
        }

        var from = DateTime.UtcNow.AddMinutes(-1);
        var to = DateTime.UtcNow.AddMinutes(1);
        var firstResponse = await _healthClient.GetHistoryPageAsync(
            nodeId,
            from,
            to,
            limit: 2);

        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(firstResponse.Data, Is.Not.Null);
        Assert.That(firstResponse.Data!.Items, Has.Count.EqualTo(2));
        Assert.That(firstResponse.Data.NextCursor, Is.Not.Null.And.Not.Empty);
        Assert.That(firstResponse.Data.PreviousCursor, Is.Null);

        var olderResponse = await _healthClient.GetHistoryPageAsync(
            nodeId,
            from,
            to,
            cursor: firstResponse.Data.NextCursor,
            limit: 2);

        Assert.That(olderResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(olderResponse.Data, Is.Not.Null);
        Assert.That(olderResponse.Data!.Items, Has.Count.EqualTo(1));
        Assert.That(olderResponse.Data.NextCursor, Is.Null);
        Assert.That(olderResponse.Data.PreviousCursor, Is.Not.Null.And.Not.Empty);

        var newerResponse = await _healthClient.GetHistoryPageAsync(
            nodeId,
            from,
            to,
            cursor: olderResponse.Data.PreviousCursor,
            previous: true,
            limit: 2);

        Assert.That(newerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(newerResponse.Data, Is.Not.Null);
        Assert.That(
            newerResponse.Data!.Items.Select(item => item.Id),
            Is.EqualTo(firstResponse.Data.Items.Select(item => item.Id)));
    }

    [Test]
    public async Task GetHistoryPage_WithReversedTimeRange_ShouldReturnBadRequest()
    {
        var response = await _healthClient.GetHistoryPageAsync(
            Guid.NewGuid(),
            from: DateTime.UtcNow,
            to: DateTime.UtcNow.AddMinutes(-1));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
