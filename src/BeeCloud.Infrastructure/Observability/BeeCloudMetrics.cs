using System.Diagnostics.Metrics;
using BeeCloud.Application.Interfaces;

namespace BeeCloud.Infrastructure.Observability;

public class BeeCloudMetrics : IBeeCloudMetrics
{
    private const string MeterName = "BeeCloud";

    private readonly Meter _meter;

    private int _nodesTotal;
    private int _nodesAvailable;
    private int _nodesUnhealthy;
    private int _openIncidents;
    private long _provisioningTotal;
    private long _provisioningFailures;
    private long _remediationTotal;
    private long _remediationFailures;
    public BeeCloudMetrics()
    {
        _meter = new Meter(MeterName);

        _meter.CreateObservableGauge(
            "beecloud_nodes_total",
            () => _nodesTotal,
            description: "Total number of compute nodes.");

        _meter.CreateObservableGauge(
            "beecloud_nodes_available",
            () => _nodesAvailable,
            description: "Number of available compute nodes.");

        _meter.CreateObservableGauge(
            "beecloud_nodes_unhealthy",
            () => _nodesUnhealthy,
            description: "Number of unhealthy compute nodes.");

        _meter.CreateObservableGauge(
            "beecloud_incidents_open",
            () => _openIncidents,
            description: "Number of open incidents.");
        _meter.CreateObservableGauge(
            "beecloud_provisioning_total",
            () => _provisioningTotal,
            description: "Total number of provisioning operations.");

        _meter.CreateObservableGauge(
            "beecloud_provisioning_failures_total",
            () => _provisioningFailures,
            description: "Total number of failed provisioning operations.");

        _meter.CreateObservableGauge(
            "beecloud_remediation_total",
            () => _remediationTotal,
            description: "Total number of successful remediation operations.");

        _meter.CreateObservableGauge(
            "beecloud_remediation_failures_total",
            () => _remediationFailures,
            description: "Total number of failed remediation operations.");
    }

    public void SetNodeCounts(
        int total,
        int available,
        int unhealthy)
    {
        _nodesTotal = total;
        _nodesAvailable = available;
        _nodesUnhealthy = unhealthy;
    }

    public void SetOpenIncidentCount(int count)
    {
        _openIncidents = count;
    }
    
    public void SetOperationalCounts(
        long provisioningTotal,
        long provisioningFailures,
        long remediationTotal,
        long remediationFailures)
    {
        _provisioningTotal = provisioningTotal;
        _provisioningFailures = provisioningFailures;
        _remediationTotal = remediationTotal;
        _remediationFailures = remediationFailures;
    }
}