import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";
import {
    deriveHealthAlerts,
    formatAlertAge,
    HIGH_GPU_TEMPERATURE_THRESHOLD,
    HIGH_USAGE_THRESHOLD,
} from "./healthAlerts";

const node: ComputeNode = {
    id: "node-1",
    name: "gpu-node-01",
    gpuModel: "NVIDIA H100",
    gpuCount: 4,
    status: "Running",
    activeFault: "None",
};

const health: HealthCheck = {
    id: "health-1",
    computeNodeId: node.id,
    isHealthy: true,
    cpuUsagePercent: 50,
    gpuUsagePercent: 60,
    gpuTemperatureCelsius: 65,
    checkedAt: "2026-10-07T12:00:00Z",
};

describe("deriveHealthAlerts", () => {
    it("formats historical alert age", () => {
        const now = new Date("2026-10-07T14:00:00");

        expect(
            formatAlertAge("2026-10-06T10:00:00", now),
        ).toBe("Yesterday");
        expect(
            formatAlertAge("2026-10-07T10:00:00", now),
        ).toBe("Today");
    });

    it("returns empty active and historical alerts below thresholds", () => {
        expect(
            deriveHealthAlerts(
                [node],
                { [node.id]: [health] },
                { [node.id]: health },
            ),
        ).toEqual({ active: [], historical: [] });
    });

    it("flags an unhealthy latest check as active", () => {
        const latest = { ...health, isHealthy: false };
        const alerts = deriveHealthAlerts(
            [node],
            { [node.id]: [latest] },
            { [node.id]: latest },
        );

        expect(alerts.active).toEqual([
            expect.objectContaining({
                id: "node-1-unhealthy-health-1",
                type: "unhealthy",
                nodeName: node.name,
                checkedAt: health.checkedAt,
                message: "Node health check reports unhealthy.",
            }),
        ]);
        expect(alerts.historical).toEqual([]);
    });

    it("flags CPU usage at or above the threshold as active", () => {
        const latest = {
            ...health,
            cpuUsagePercent: HIGH_USAGE_THRESHOLD,
        };
        const alerts = deriveHealthAlerts(
            [node],
            { [node.id]: [latest] },
            { [node.id]: latest },
        );

        expect(alerts.active).toEqual([
            expect.objectContaining({
                type: "high-cpu",
                message: "CPU usage is high (90.0%).",
            }),
        ]);
    });

    it("flags GPU usage above the threshold as active", () => {
        const latest = { ...health, gpuUsagePercent: 94.25 };
        const alerts = deriveHealthAlerts(
            [node],
            { [node.id]: [latest] },
            { [node.id]: latest },
        );

        expect(alerts.active).toEqual([
            expect.objectContaining({
                type: "high-gpu",
                message: "GPU usage is high (94.3%).",
            }),
        ]);
    });

    it("flags GPU temperature at or above 90 degrees as active", () => {
        const latest = {
            ...health,
            gpuTemperatureCelsius: HIGH_GPU_TEMPERATURE_THRESHOLD,
        };
        const alerts = deriveHealthAlerts(
            [node],
            { [node.id]: [latest] },
            { [node.id]: latest },
        );

        expect(alerts.active).toEqual([
            expect.objectContaining({
                type: "high-temperature",
                message: "GPU temperature is high (90.0°C).",
            }),
        ]);
    });

    it("moves resolved alert conditions to history and keeps their latest occurrence", () => {
        const previous: HealthCheck = {
            ...health,
            id: "health-old",
            isHealthy: false,
            gpuTemperatureCelsius: 94,
            checkedAt: "2026-10-06T12:00:00Z",
        };
        const latest: HealthCheck = {
            ...health,
            checkedAt: "2026-10-07T12:00:00Z",
        };
        const older: HealthCheck = {
            ...previous,
            id: "health-older",
            gpuTemperatureCelsius: 92,
            checkedAt: "2026-10-05T12:00:00Z",
        };

        const alerts = deriveHealthAlerts(
            [node],
            { [node.id]: [older, previous, latest] },
            { [node.id]: latest },
        );

        expect(alerts.active).toEqual([]);
        expect(alerts.historical.map(alert => alert.type)).toEqual([
            "unhealthy",
            "high-temperature",
        ]);
        expect(alerts.historical.map(alert => alert.checkedAt)).toEqual([
            previous.checkedAt,
            previous.checkedAt,
        ]);
        expect(alerts.historical[1].message).toBe(
            "GPU temperature reached 94.0°C.",
        );
    });

    it("classifies a non-monitored node's latest alerts as historical", () => {
        const stoppedNode = {
            ...node,
            status: "Stopped" as const,
        };
        const latest = {
            ...health,
            isHealthy: false,
            cpuUsagePercent: 99,
            gpuUsagePercent: 99,
            gpuTemperatureCelsius: 96,
        };
        const alerts = deriveHealthAlerts(
            [stoppedNode],
            { [stoppedNode.id]: [latest] },
            { [stoppedNode.id]: latest },
        );

        expect(alerts.active).toEqual([]);
        expect(alerts.historical.map(alert => alert.type)).toEqual([
            "unhealthy",
            "high-cpu",
            "high-gpu",
            "high-temperature",
        ]);
    });

    it("does not create usage alerts for null metrics", () => {
        const latest = {
            ...health,
            cpuUsagePercent: null,
            gpuUsagePercent: null,
            gpuTemperatureCelsius: null,
        };
        const alerts = deriveHealthAlerts(
            [node],
            { [node.id]: [latest] },
            { [node.id]: latest },
        );

        expect(alerts).toEqual({ active: [], historical: [] });
    });

    it("does not alert just below the thresholds", () => {
        const latest = {
            ...health,
            cpuUsagePercent: HIGH_USAGE_THRESHOLD - 0.1,
            gpuUsagePercent: HIGH_USAGE_THRESHOLD - 0.1,
            gpuTemperatureCelsius:
                HIGH_GPU_TEMPERATURE_THRESHOLD - 0.1,
        };

        expect(
            deriveHealthAlerts(
                [node],
                { [node.id]: [latest] },
                { [node.id]: latest },
            ),
        ).toEqual({ active: [], historical: [] });
    });
});
