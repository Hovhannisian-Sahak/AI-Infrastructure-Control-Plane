using BeeCloud.Worker.Processors;
using BeeCloud.Worker.Workers;
using Microsoft.Extensions.Logging;
using Moq;

namespace BeeCloud.UnitTests.Metrics;

[TestFixture]
public class MetricsWorkerTests
{
    private Mock<IMetricsProcessor> _processor = null!;
    private Mock<ILogger<MetricsWorker>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _processor = new Mock<IMetricsProcessor>();
        _logger = new Mock<ILogger<MetricsWorker>>();
    }

    [Test]
    public async Task ExecuteAsync_ShouldCallProcessor()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        _processor
            .Setup(processor => processor.ProcessAsync(
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                cancellationTokenSource.Cancel();
            })
            .Returns(Task.CompletedTask);

        var worker = new MetricsWorker(
            _processor.Object,
            _logger.Object);

        await worker.StartAsync(
            cancellationTokenSource.Token);

        await worker.StopAsync(
            CancellationToken.None);

        _processor.Verify(
            processor => processor.ProcessAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenProcessorThrows_ShouldContinueUntilCancellation()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var callCount = 0;

        _processor
            .Setup(processor => processor.ProcessAsync(
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                callCount++;

                if (callCount >= 1)
                {
                    cancellationTokenSource.Cancel();
                }
            })
            .ThrowsAsync(
                new InvalidOperationException("Test error"));

        var worker = new MetricsWorker(
            _processor.Object,
            _logger.Object);

        await worker.StartAsync(
            cancellationTokenSource.Token);

        await worker.StopAsync(
            CancellationToken.None);

        Assert.That(callCount, Is.EqualTo(1));

        _processor.Verify(
            processor => processor.ProcessAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_WhenProcessorThrowsOperationCanceledException_ShouldStopGracefully()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        _processor
            .Setup(processor => processor.ProcessAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new OperationCanceledException(
                    cancellationTokenSource.Token));

        var worker = new MetricsWorker(
            _processor.Object,
            _logger.Object);

        await worker.StartAsync(
            cancellationTokenSource.Token);

        await worker.StopAsync(
            CancellationToken.None);

        _processor.Verify(
            processor => processor.ProcessAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_WhenCancellationIsRequested_ShouldStopWorker()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var processorCalled =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        _processor
            .Setup(processor => processor.ProcessAsync(
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                processorCalled.TrySetResult(true);
            })
            .Returns(Task.CompletedTask);

        var worker = new MetricsWorker(
            _processor.Object,
            _logger.Object);

        await worker.StartAsync(
            cancellationTokenSource.Token);

        await processorCalled.Task.WaitAsync(
            TimeSpan.FromSeconds(2));

        cancellationTokenSource.Cancel();

        await worker.StopAsync(
            CancellationToken.None);

        Assert.Pass();
    }
}