import type { ComputeNode } from "@/lib/api/models/computeNode";
import {
    getHealthMonitoringNodeIds,
    isHealthMonitored,
} from "./healthMonitoring";

const node = (id: string, status: ComputeNode["status"]): ComputeNode => ({
    id,
    name: id,
    gpuModel: "NVIDIA H100",
    gpuCount: 1,
    status,
    activeFault: "None",
});

describe("healthMonitoring", () => {
    it.each([
        ["Running", true],
        ["Unhealthy", true],
        ["Quarantined", true],
        ["Remediating", true],
        ["Provisioning", false],
        ["Available", false],
        ["Stopping", false],
        ["Stopped", false],
        ["Failed", false],
    ] as const)(
        "returns whether %s nodes are monitored",
        (status, monitored) => {
            expect(isHealthMonitored(status)).toBe(monitored);
        },
    );

    it("separates monitored nodes from nodes whose history should be retained", () => {
        const nodes = [
            node("stopped", "Stopped"),
            node("running", "Running"),
            node("available", "Available"),
        ];

        expect(getHealthMonitoringNodeIds(nodes, true)).toEqual([
            "running",
        ]);
        expect(getHealthMonitoringNodeIds(nodes, false)).toEqual([
            "available",
            "stopped",
        ]);
    });
});
