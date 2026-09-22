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
}