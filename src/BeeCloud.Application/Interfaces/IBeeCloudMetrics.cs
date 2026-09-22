namespace BeeCloud.Application.Interfaces;

public interface IBeeCloudMetrics
{
    void SetNodeCounts(
        int total,
        int available,
        int unhealthy);

    void SetOpenIncidentCount(
        int count);
}