using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.IntegrationTests.Infrastructure;
using BeeCloud.Worker.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace BeeCloud.IntegrationTests.Remediation;

[TestFixture]
public class RemediationProcessorIntegrationTests
{
    private PostgreSqlTestContainer _postgres = null!;
    private ApplicationDbContext _dbContext = null!;
    private RemediationProcessor _processor = null!;

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

        IIncidentRepository incidentRepository =
            new IncidentRepository(_dbContext);

        IIncidentService incidentService =
            new IncidentService(
                incidentRepository,
                nodeRepository);

        _processor = new RemediationProcessor(
            nodeRepository,
            incidentService,
            NullLogger<RemediationProcessor>.Instance);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task ProcessAsync_WithQuarantinedNode_ShouldStartRemediationAndRecover()
    {
        var node = await CreateNodeAsync(NodeStatus.Quarantined);

        await _processor.ProcessAsync();

        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Available));
    }

    [Test]
    public async Task ProcessAsync_WithQuarantinedNode_ShouldClearFault()
    {
        var node = await CreateNodeAsync(NodeStatus.Quarantined);

        node.SimulateFault(NodeFault.GpuOverheat);

        await _dbContext.SaveChangesAsync();

        await _processor.ProcessAsync();

        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.ActiveFault,
            Is.EqualTo(NodeFault.None));
    }

    [Test]
    public async Task ProcessAsync_WithSuccessfulRemediation_ShouldResolveIncident()
    {
        var node = await CreateNodeAsync(NodeStatus.Quarantined);

        node.SimulateFault(NodeFault.GpuOverheat);

        var incident = new Incident(
            node.Id,
            IncidentSeverity.Critical,
            "GPU overheat detected",
            "GPU temperature exceeded the critical threshold.");

        await _dbContext.Incidents.AddAsync(incident);
        await _dbContext.SaveChangesAsync();

        await _processor.ProcessAsync();

        var persistedIncident = await _dbContext.Incidents
            .AsNoTracking()
            .FirstAsync(i => i.Id == incident.Id);

        Assert.That(
            persistedIncident.Status,
            Is.EqualTo(IncidentStatus.Resolved));

        Assert.That(
            persistedIncident.ResolvedAt,
            Is.Not.Null);
    }

    [Test]
    public async Task ProcessAsync_WithServiceCrash_ShouldMarkNodeAsFailed()
    {
        var node = await CreateNodeAsync(NodeStatus.Quarantined);

        node.SimulateFault(NodeFault.ServiceCrash);

        await _dbContext.SaveChangesAsync();

        await _processor.ProcessAsync();

        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Failed));
    }

    [Test]
    public async Task ProcessAsync_WithServiceCrash_ShouldKeepFault()
    {
        var node = await CreateNodeAsync(NodeStatus.Quarantined);

        node.SimulateFault(NodeFault.ServiceCrash);

        await _dbContext.SaveChangesAsync();

        await _processor.ProcessAsync();

        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.ActiveFault,
            Is.EqualTo(NodeFault.ServiceCrash));
    }

    [Test]
    public async Task ProcessAsync_WithNoUnhealthyOrQuarantinedNodes_ShouldDoNothing()
    {
        var node = await CreateNodeAsync(NodeStatus.Available);

        await _processor.ProcessAsync();

        var persistedNode = await _dbContext.ComputeNodes
            .AsNoTracking()
            .FirstAsync(n => n.Id == node.Id);

        Assert.That(
            persistedNode.Status,
            Is.EqualTo(NodeStatus.Available));
    }

    private async Task<ComputeNode> CreateNodeAsync(
        NodeStatus status)
    {
        var node = new ComputeNode(
            $"remediation-test-node-{Guid.NewGuid():N}",
            "NVIDIA A100",
            1);

        switch (status)
        {
            case NodeStatus.Unhealthy:
                node.MarkAvailable();
                node.Start();
                node.MarkUnhealthy();
                break;

            case NodeStatus.Quarantined:
                node.MarkAvailable();
                node.Start();
                node.MarkUnhealthy();
                node.Quarantine();
                break;

            case NodeStatus.Available:
                node.MarkAvailable();
                break;

            default:
                throw new ArgumentException(
                    $"Unsupported test status: {status}",
                    nameof(status));
        }

        await _dbContext.ComputeNodes.AddAsync(node);
        await _dbContext.SaveChangesAsync();

        return node;
    }
}