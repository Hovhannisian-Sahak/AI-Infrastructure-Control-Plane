import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";
import {
    deriveHealthAlerts,
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
    it("returns no alerts for healthy metrics below the usage threshold", () => {
        expect(
            deriveHealthAlerts([node], {
                [node.id]: health,
            }),
        ).toEqual([]);
    });

    it("flags an unhealthy health check", () => {
        const alerts = deriveHealthAlerts([node], {
            [node.id]: {
                ...health,
                isHealthy: false,
            },
        });

        expect(alerts).toEqual([
            expect.objectContaining({
                id: "node-1-unhealthy",
                type: "unhealthy",
                nodeName: node.name,
                checkedAt: health.checkedAt,
            }),
        ]);
    });

    it("flags CPU usage at or above the threshold", () => {
        const alerts = deriveHealthAlerts([node], {
            [node.id]: {
                ...health,
                cpuUsagePercent: HIGH_USAGE_THRESHOLD,
            },
        });

        expect(alerts).toEqual([
            expect.objectContaining({
                type: "high-cpu",
                message: "CPU usage is high (90.0%).",
            }),
        ]);
    });

    it("flags GPU usage above the threshold", () => {
        const alerts = deriveHealthAlerts([node], {
            [node.id]: {
                ...health,
                gpuUsagePercent: 94.25,
            },
        });

        expect(alerts).toEqual([
            expect.objectContaining({
                type: "high-gpu",
                message: "GPU usage is high (94.3%).",
            }),
        ]);
    });

    it("returns all applicable alerts for a node", () => {
        const alerts = deriveHealthAlerts([node], {
            [node.id]: {
                ...health,
                isHealthy: false,
                cpuUsagePercent: 95,
                gpuUsagePercent: 96,
            },
        });

        expect(alerts.map(alert => alert.type)).toEqual([
            "unhealthy",
            "high-cpu",
            "high-gpu",
        ]);
    });

    it("ignores absent health data and null metrics", () => {
        expect(
            deriveHealthAlerts(
                [node, { ...node, id: "node-2" }],
                {
                    [node.id]: {
                        ...health,
                        cpuUsagePercent: null,
                        gpuUsagePercent: null,
                    },
                },
            ),
        ).toEqual([]);
    });

    it("does not flag usage just below the threshold", () => {
        const alerts = deriveHealthAlerts([node], {
            [node.id]: {
                ...health,
                cpuUsagePercent: HIGH_USAGE_THRESHOLD - 0.1,
                gpuUsagePercent: HIGH_USAGE_THRESHOLD - 0.1,
            },
        });

        expect(alerts).toEqual([]);
    });
});
