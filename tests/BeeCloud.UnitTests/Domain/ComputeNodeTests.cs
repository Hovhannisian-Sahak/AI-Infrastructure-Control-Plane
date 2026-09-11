using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Domain.Exceptions;
using NUnit.Framework;

namespace BeeCloud.UnitTests.Domain;

[TestFixture]
public class ComputeNodeTests
{
    [Test]
    public void NewNode_ShouldStartInProvisioning()
    {
        // Arrange
        var node = new ComputeNode(
            "gpu-node-001",
            "B200",
            8);

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Provisioning));
    }

    [Test]
    public void MarkAvailable_WhenProvisioning_ShouldBecomeAvailable()
    {
        // Arrange
        var node = CreateNode();

        // Act
        node.MarkAvailable();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Available));
    }

    [Test]
    public void Start_WhenAvailable_ShouldBecomeRunning()
    {
        // Arrange
        var node = CreateNode();
        node.MarkAvailable();

        // Act
        node.Start();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Running));
    }

    [Test]
    public void Stop_WhenRunning_ShouldBecomeStopping()
    {
        // Arrange
        var node = CreateRunningNode();

        // Act
        node.Stop();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Stopping));
    }

    [Test]
    public void CompleteStopping_WhenStopping_ShouldBecomeStopped()
    {
        // Arrange
        var node = CreateRunningNode();
        node.Stop();

        // Act
        node.CompleteStopping();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Stopped));
    }

    [Test]
    public void Start_WhenAlreadyRunning_ShouldThrow()
    {
        // Arrange
        var node = CreateRunningNode();

        // Act & Assert
        Assert.Throws<InvalidNodeStateTransitionException>(
            () => node.Start());
    }

    [Test]
    public void MarkAvailable_WhenRunning_ShouldThrow()
    {
        // Arrange
        var node = CreateRunningNode();

        // Act & Assert
        Assert.Throws<InvalidNodeStateTransitionException>(
            () => node.MarkAvailable());
    }

    [Test]
    public void RunningNode_WhenUnhealthy_ShouldBecomeUnhealthy()
    {
        // Arrange
        var node = CreateRunningNode();

        // Act
        node.MarkUnhealthy();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Unhealthy));
    }

    [Test]
    public void UnhealthyNode_WhenQuarantined_ShouldBecomeQuarantined()
    {
        // Arrange
        var node = CreateRunningNode();
        node.MarkUnhealthy();

        // Act
        node.Quarantine();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Quarantined));
    }

    [Test]
    public void QuarantinedNode_WhenRemediationStarts_ShouldBecomeRemediating()
    {
        // Arrange
        var node = CreateRunningNode();

        node.MarkUnhealthy();
        node.Quarantine();

        // Act
        node.StartRemediation();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Remediating));
    }

    [Test]
    public void RemediatingNode_WhenRecovered_ShouldBecomeAvailable()
    {
        // Arrange
        var node = CreateRunningNode();

        node.MarkUnhealthy();
        node.Quarantine();
        node.StartRemediation();

        // Act
        node.Recover();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Available));
    }

    [Test]
    public void QuarantinedNode_WhenRemediationFails_ShouldBecomeFailed()
    {
        // Arrange
        var node = CreateRunningNode();

        node.MarkUnhealthy();
        node.Quarantine();

        // Act
        node.MarkFailed();

        // Assert
        Assert.That(
            node.Status,
            Is.EqualTo(NodeStatus.Failed));
    }
    
    [Test]
    public void Start_WhenProvisioning_ShouldThrow()
    {
        // Arrange
        var node = CreateNode();

        // Act & Assert
        Assert.Throws<InvalidNodeStateTransitionException>(
            () => node.Start());
    }

    [Test]
    public void Start_WhenStopped_ShouldThrow()
    {
        // Arrange
        var node = CreateRunningNode();

        node.Stop();
        node.CompleteStopping();

        // Act & Assert
        Assert.Throws<InvalidNodeStateTransitionException>(
            () => node.Start());
    }

    [Test]
    public void Stop_WhenAvailable_ShouldThrow()
    {
        // Arrange
        var node = CreateNode();
        node.MarkAvailable();

        // Act & Assert
        Assert.Throws<InvalidNodeStateTransitionException>(
            () => node.Stop());
    }
    [Test]
    public void RecordHealthCheck_ShouldUpdateLastHealthCheck()
    {
        // Arrange
        var node = CreateNode();
        var timestamp = DateTime.UtcNow;

        // Act
        node.RecordHealthCheck(timestamp);

        // Assert
        Assert.That(
            node.LastHealthCheck,
            Is.EqualTo(timestamp));
    }

    private static ComputeNode CreateNode()
    {
        return new ComputeNode(
            "gpu-node-001",
            "B200",
            8);
    }

    private static ComputeNode CreateRunningNode()
    {
        var node = CreateNode();

        node.MarkAvailable();
        node.Start();

        return node;
    }
}