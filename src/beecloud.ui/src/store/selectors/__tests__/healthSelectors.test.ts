import {
    selectHealthHistoryByNodeId,
    selectLatestHealthByNodeId,
    selectHealthLoadingByNodeId,
    selectHealthErrorByNodeId,
    selectHealthyHealthChecks,
    selectUnhealthyHealthChecks,
    selectCpuHistory,
    selectGpuHistory,
    selectTemperatureHistory,
    selectHealthHistorySince,
} from "../healthSelectors";

import type { HealthCheck } from "@/lib/api/models/healthCheck";
import { createTestStore } from "@/test-utils";

const health1: HealthCheck = {
    id: "health-1",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 40,
    gpuUsagePercent: 60,
    gpuTemperatureCelsius: 65,
    checkedAt: "2026-10-07T10:00:00Z",
};

const health2: HealthCheck = {
    id: "health-2",
    computeNodeId: "node-1",
    isHealthy: false,
    cpuUsagePercent: 95,
    gpuUsagePercent: 80,
    gpuTemperatureCelsius: 96,
    checkedAt: "2026-10-07T11:00:00Z",
};

const health3: HealthCheck = {
    id: "health-3",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: null,
    gpuUsagePercent: 70,
    gpuTemperatureCelsius: null,
    checkedAt: "2026-10-07T12:00:00Z",
};

const state = createTestStore({
    health: {
        historyByNodeId: {
            "node-1": [
                health1,
                health2,
                health3,
            ],
        },
        latestByNodeId: {
            "node-1": health3,
        },
        loadingByNodeId: {
            "node-1": false,
        },
        errorByNodeId: {
            "node-1": null,
        },
    },
}).getState();

describe("healthSelectors", () => {
    it("selects health history by node", () => {
        expect(
            selectHealthHistoryByNodeId(
                state,
                "node-1",
            ),
        ).toEqual([
            health1,
            health2,
            health3,
        ]);
    });

    it("returns a stable empty history for an unknown node", () => {
        const first = selectHealthHistoryByNodeId(
            state,
            "unknown",
        );

        const second = selectHealthHistoryByNodeId(
            state,
            "unknown",
        );

        expect(first).toEqual([]);
        expect(first).toBe(second);
    });

    it("selects latest health", () => {
        expect(
            selectLatestHealthByNodeId(
                state,
                "node-1",
            ),
        ).toEqual(health3);
    });

    it("returns null for missing latest health", () => {
        expect(
            selectLatestHealthByNodeId(
                state,
                "unknown",
            ),
        ).toBeNull();
    });

    it("selects loading state", () => {
        expect(
            selectHealthLoadingByNodeId(
                state,
                "node-1",
            ),
        ).toBe(false);
    });

    it("selects error state", () => {
        expect(
            selectHealthErrorByNodeId(
                state,
                "node-1",
            ),
        ).toBeNull();
    });

    it("filters healthy checks", () => {
        expect(
            selectHealthyHealthChecks([
                health1,
                health2,
                health3,
            ]),
        ).toEqual([
            health1,
            health3,
        ]);
    });

    it("filters unhealthy checks", () => {
        expect(
            selectUnhealthyHealthChecks([
                health1,
                health2,
                health3,
            ]),
        ).toEqual([
            health2,
        ]);
    });

    it("creates CPU history", () => {
        expect(
            selectCpuHistory([
                health1,
                health2,
                health3,
            ]),
        ).toEqual([
            40,
            95,
            null,
        ]);
    });

    it("creates GPU history", () => {
        expect(
            selectGpuHistory([
                health1,
                health2,
                health3,
            ]),
        ).toEqual([
            60,
            80,
            70,
        ]);
    });

    it("creates temperature history", () => {
        expect(
            selectTemperatureHistory([
                health1,
                health2,
                health3,
            ]),
        ).toEqual([
            65,
            96,
            null,
        ]);
    });

    it("filters history by start time", () => {
        const result = selectHealthHistorySince(
            [
                health1,
                health2,
                health3,
            ],
            new Date("2026-10-07T10:30:00Z"),
        );

        expect(result).toEqual([
            health2,
            health3,
        ]);
    });
});