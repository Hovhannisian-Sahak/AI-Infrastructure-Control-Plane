import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";
import { isHealthMonitored } from "./healthMonitoring";

export const HIGH_USAGE_THRESHOLD = 90;

export type HealthAlertType =
    | "unhealthy"
    | "high-cpu"
    | "high-gpu";

export type HealthAlert = {
    id: string;
    nodeId: string;
    nodeName: string;
    type: HealthAlertType;
    message: string;
    checkedAt: string;
};

export function deriveHealthAlerts(
    nodes: ComputeNode[],
    latestByNodeId: Record<
        string,
        HealthCheck | null | undefined
    >,
): HealthAlert[] {
    const alerts: HealthAlert[] = [];

    for (const node of nodes) {
        if (!isHealthMonitored(node.status)) {
            continue;
        }

        const latest = latestByNodeId[node.id];

        if (!latest) {
            continue;
        }

        if (!latest.isHealthy) {
            alerts.push({
                id: `${node.id}-unhealthy`,
                nodeId: node.id,
                nodeName: node.name,
                type: "unhealthy",
                message: "Node health check reports unhealthy.",
                checkedAt: latest.checkedAt,
            });
        }

        if (
            latest.cpuUsagePercent !== null &&
            latest.cpuUsagePercent >= HIGH_USAGE_THRESHOLD
        ) {
            alerts.push({
                id: `${node.id}-high-cpu`,
                nodeId: node.id,
                nodeName: node.name,
                type: "high-cpu",
                message: `CPU usage is high (${latest.cpuUsagePercent.toFixed(1)}%).`,
                checkedAt: latest.checkedAt,
            });
        }

        if (
            latest.gpuUsagePercent !== null &&
            latest.gpuUsagePercent >= HIGH_USAGE_THRESHOLD
        ) {
            alerts.push({
                id: `${node.id}-high-gpu`,
                nodeId: node.id,
                nodeName: node.name,
                type: "high-gpu",
                message: `GPU usage is high (${latest.gpuUsagePercent.toFixed(1)}%).`,
                checkedAt: latest.checkedAt,
            });
        }
    }

    return alerts;
}
