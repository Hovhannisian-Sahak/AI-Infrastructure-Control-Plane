using System.Net;
using BeeCloud.ApiTests.Clients;
using BeeCloud.ApiTests.TestData;

namespace BeeCloud.ApiTests;

[TestFixture]
public class NodeMetricsApiTests
{
    private NodesClient _nodesClient = null!;
    private NodeMetricsClient _metricsClient = null!;
    private ApiTestDataHelper _testData = null!;

    [SetUp]
    public void SetUp()
    {
        var baseUrl = TestConfiguration.BaseUrl;

        _nodesClient = new NodesClient(baseUrl);
        _metricsClient = new NodeMetricsClient(baseUrl);
        _testData = new ApiTestDataHelper(_nodesClient);
    }

    [Test]
    public async Task GetById_WhenMetricDoesNotExist_ShouldReturnNotFound()
    {
        var response = await _metricsClient.GetByIdAsync(Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetHistory_WhenRangeHasNoMetrics_ShouldReturnEmptyList()
    {
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var from = DateTime.UtcNow.AddDays(30);
        var to = from.AddDays(1);

        var response = await _metricsClient.GetHistoryAsync(
            nodeId,
            from,
            to,
            limit: 10);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Null);
        Assert.That(response.Data, Is.Empty);
    }

    [Test]
    public async Task GetHistoryPage_WhenRangeHasNoMetrics_ShouldReturnEmptyPage()
    {
        var nodeId = await _testData.CreateAvailableNodeAsync();
        var from = DateTime.UtcNow.AddDays(30);
        var to = from.AddDays(1);

        var response = await _metricsClient.GetHistoryPageAsync(
            nodeId,
            from,
            to,
            limit: 10);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Data, Is.Not.Null);
        Assert.That(response.Data!.Items, Is.Empty);
        Assert.That(response.Data.NextCursor, Is.Null);
        Assert.That(response.Data.PreviousCursor, Is.Null);
    }

    [Test]
    public async Task GetHistoryPage_WhenNodeDoesNotExist_ShouldReturnNotFound()
    {
        var response = await _metricsClient.GetHistoryPageAsync(Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetHistoryPage_WithInvalidLimit_ShouldReturnBadRequest()
    {
        var response = await _metricsClient.GetHistoryPageAsync(
            Guid.NewGuid(),
            limit: 0);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetHistoryPage_WithReversedTimeRange_ShouldReturnBadRequest()
    {
        var response = await _metricsClient.GetHistoryPageAsync(
            Guid.NewGuid(),
            from: DateTime.UtcNow,
            to: DateTime.UtcNow.AddMinutes(-1));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
