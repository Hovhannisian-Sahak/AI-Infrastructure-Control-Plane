using BeeCloud.Domain.Entities;
using BeeCloud.Domain.Enums;
using NUnit.Framework;

namespace BeeCloud.UnitTests.Domain;

[TestFixture]
public class IncidentTests
{
    [Test]
    public void NewIncident_ShouldStartAsOpen()
    {
        var incident = CreateIncident();

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Open));
    }

    [Test]
    public void NewIncident_ShouldStoreSeverity()
    {
        var incident = new Incident(
            Guid.NewGuid(),
            IncidentSeverity.Critical,
            "GPU overheating");

        Assert.That(
            incident.Severity,
            Is.EqualTo(IncidentSeverity.Critical));
    }

    [Test]
    public void StartInvestigation_WhenOpen_ShouldBecomeInvestigating()
    {
        var incident = CreateIncident();

        incident.StartInvestigation();

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Investigating));
    }

    [Test]
    public void Resolve_WhenOpen_ShouldBecomeResolved()
    {
        var incident = CreateIncident();

        incident.Resolve();

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Resolved));

        Assert.That(
            incident.ResolvedAt,
            Is.Not.Null);
    }

    [Test]
    public void Resolve_WhenInvestigating_ShouldBecomeResolved()
    {
        var incident = CreateIncident();

        incident.StartInvestigation();
        incident.Resolve();

        Assert.That(
            incident.Status,
            Is.EqualTo(IncidentStatus.Resolved));
    }

    [Test]
    public void StartInvestigation_WhenAlreadyInvestigating_ShouldThrow()
    {
        var incident = CreateIncident();

        incident.StartInvestigation();

        Assert.Throws<InvalidOperationException>(
            () => incident.StartInvestigation());
    }

    [Test]
    public void Resolve_WhenAlreadyResolved_ShouldThrow()
    {
        var incident = CreateIncident();

        incident.Resolve();

        Assert.Throws<InvalidOperationException>(
            () => incident.Resolve());
    }

    [Test]
    public void NewIncident_WithEmptyNodeId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new Incident(
                Guid.Empty,
                IncidentSeverity.High,
                "GPU failure"));
    }

    [Test]
    public void NewIncident_WithEmptyTitle_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new Incident(
                Guid.NewGuid(),
                IncidentSeverity.High,
                ""));
    }

    private static Incident CreateIncident()
    {
        return new Incident(
            Guid.NewGuid(),
            IncidentSeverity.High,
            "GPU failure",
            "GPU became unavailable.");
    }
}