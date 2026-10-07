import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";
import { isHealthMonitored } from "./healthMonitoring";

export const HIGH_USAGE_THRESHOLD = 90;
export const HIGH_GPU_TEMPERATURE_THRESHOLD = 90;

export type HealthAlertType =
    | "unhealthy"
    | "high-cpu"
    | "high-gpu"
    | "high-temperature";

export type HealthAlert = {
    id: string;
    nodeId: string;
    nodeName: string;
    type: HealthAlertType;
    message: string;
    checkedAt: string;
};

export type DerivedHealthAlerts = {
    active: HealthAlert[];
    historical: HealthAlert[];
};

function alertsForReading(
    node: ComputeNode,
    health: HealthCheck,
    historical: boolean,
): HealthAlert[] {
    const alerts: HealthAlert[] = [];
    const suffix = historical ? " reached" : " is high";
    const createAlert = (
        type: HealthAlertType,
        text: string,
    ): HealthAlert => ({
        id: `${node.id}-${type}-${health.id}`,
        nodeId: node.id,
        nodeName: node.name,
        type,
        message: text,
        checkedAt: health.checkedAt,
    });

    if (!health.isHealthy) {
        alerts.push(
            createAlert(
                "unhealthy",
                historical
                    ? "Node health check reported unhealthy."
                    : "Node health check reports unhealthy.",
            ),
        );
    }

    if (
        health.cpuUsagePercent !== null &&
        health.cpuUsagePercent >= HIGH_USAGE_THRESHOLD
    ) {
        const value = `${health.cpuUsagePercent.toFixed(1)}%`;
        alerts.push(
            createAlert(
                "high-cpu",
                historical
                    ? `CPU usage reached ${value}.`
                    : `CPU usage${suffix} (${value}).`,
            ),
        );
    }

    if (
        health.gpuUsagePercent !== null &&
        health.gpuUsagePercent >= HIGH_USAGE_THRESHOLD
    ) {
        const value = `${health.gpuUsagePercent.toFixed(1)}%`;
        alerts.push(
            createAlert(
                "high-gpu",
                historical
                    ? `GPU usage reached ${value}.`
                    : `GPU usage${suffix} (${value}).`,
            ),
        );
    }

    if (
        health.gpuTemperatureCelsius !== null &&
        health.gpuTemperatureCelsius >= HIGH_GPU_TEMPERATURE_THRESHOLD
    ) {
        const value = `${health.gpuTemperatureCelsius.toFixed(1)}°C`;
        alerts.push(
            createAlert(
                "high-temperature",
                historical
                    ? `GPU temperature reached ${value}.`
                    : `GPU temperature${suffix} (${value}).`,
            ),
        );
    }

    return alerts;
}

export function deriveHealthAlerts(
    nodes: ComputeNode[],
    historyByNodeId: Record<string, HealthCheck[]>,
    latestByNodeId: Record<
        string,
        HealthCheck | null | undefined
    >,
): DerivedHealthAlerts {
    const active: HealthAlert[] = [];
    const historical: HealthAlert[] = [];

    for (const node of nodes) {
        const latest = latestByNodeId[node.id] ?? null;
        const activeByType = new Set<HealthAlertType>();
        const monitored = isHealthMonitored(node.status);

        if (latest && monitored) {
            const currentAlerts = alertsForReading(node, latest, false);
            active.push(...currentAlerts);
            currentAlerts.forEach(alert => activeByType.add(alert.type));
        }

        const readings = [...(historyByNodeId[node.id] ?? [])]
            .filter(reading => reading.id !== latest?.id)
            .sort(
                (a, b) =>
                    new Date(b.checkedAt).getTime() -
                    new Date(a.checkedAt).getTime(),
            );
        const historicalByType = new Set<HealthAlertType>();

        for (const reading of readings) {
            for (const alert of alertsForReading(node, reading, true)) {
                if (
                    activeByType.has(alert.type) ||
                    historicalByType.has(alert.type)
                ) {
                    continue;
                }

                historical.push(alert);
                historicalByType.add(alert.type);
            }
        }

        if (latest && !monitored) {
            for (const alert of alertsForReading(node, latest, true)) {
                if (historicalByType.has(alert.type)) {
                    continue;
                }

                historical.push(alert);
                historicalByType.add(alert.type);
            }
        }
    }

    historical.sort(
        (a, b) =>
            new Date(b.checkedAt).getTime() -
            new Date(a.checkedAt).getTime(),
    );

    return { active, historical };
}

export function formatAlertAge(
    checkedAt: string,
    now = new Date(),
): string {
    const checkedDate = new Date(checkedAt);
    const currentDay = new Date(
        now.getFullYear(),
        now.getMonth(),
        now.getDate(),
    ).getTime();
    const alertDay = new Date(
        checkedDate.getFullYear(),
        checkedDate.getMonth(),
        checkedDate.getDate(),
    ).getTime();
    const daysAgo = Math.floor(
        (currentDay - alertDay) / (24 * 60 * 60 * 1000),
    );

    if (daysAgo <= 0) {
        return "Today";
    }

    if (daysAgo === 1) {
        return "Yesterday";
    }

    return `${daysAgo} days ago`;
}
