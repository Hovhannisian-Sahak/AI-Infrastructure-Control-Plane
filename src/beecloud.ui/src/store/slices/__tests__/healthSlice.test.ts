import {
    configureStore,
} from "@reduxjs/toolkit";

import healthReducer, {
    clearAllHealth,
    clearNodeHealth,
    fetchHealthHistory,
    fetchLatestHealth,
} from "../healthSlice";

import { nodesApi } from "@/lib/api/nodesApi";
import type { HealthCheck } from "@/lib/api/models/healthCheck";

jest.mock("@/lib/api/nodesApi");

const mockedNodesApi = jest.mocked(nodesApi);

const health1: HealthCheck = {
    id: "health-1",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 60,
    gpuUsagePercent: 70,
    gpuTemperatureCelsius: 68,
    checkedAt: "2026-10-06T16:20:00.000Z",
};

const health2: HealthCheck = {
    id: "health-2",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 65,
    gpuUsagePercent: 72,
    gpuTemperatureCelsius: 70,
    checkedAt: "2026-10-06T16:20:10.000Z",
};

const health3: HealthCheck = {
    id: "health-3",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 66,
    gpuUsagePercent: 71.2,
    gpuTemperatureCelsius: 74,
    checkedAt: "2026-10-06T16:20:30.000Z",
};

const createStore = () =>
    configureStore({
        reducer: {
            health: healthReducer,
        },
    });

describe("healthSlice", () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it("returns the initial state", () => {
        const state = healthReducer(undefined, {
            type: "unknown",
        });

        expect(state).toEqual({
            historyByNodeId: {},
            latestByNodeId: {},
            loadingByNodeId: {},
            errorByNodeId: {},
        });
    });

    it("stores health history sorted chronologically", async () => {
        mockedNodesApi.getHealthHistory.mockResolvedValue([
            health3,
            health1,
            health2,
        ]);

        const store = createStore();

        await store.dispatch(
            fetchHealthHistory({
                nodeId: "node-1",
                limit: 10,
            }),
        );

        const history =
            store.getState().health.historyByNodeId["node-1"];

        expect(history).toEqual([
            health1,
            health2,
            health3,
        ]);
    });

    it("limits health history to the requested number of latest readings", async () => {
        const readings: HealthCheck[] = Array.from(
            { length: 15 },
            (_, index) => ({
                id: `health-${index + 1}`,
                computeNodeId: "node-1",
                isHealthy: true,
                cpuUsagePercent: index,
                gpuUsagePercent: index + 10,
                gpuTemperatureCelsius: index + 60,
                checkedAt: new Date(
                    Date.parse(
                        "2026-10-06T16:20:00.000Z",
                    ) +
                    index * 1000,
                ).toISOString(),
            }),
        );

        mockedNodesApi.getHealthHistory.mockResolvedValue(
            readings,
        );

        const store = createStore();

        await store.dispatch(
            fetchHealthHistory({
                nodeId: "node-1",
                limit: 10,
            }),
        );

        const history =
            store.getState().health.historyByNodeId["node-1"];

        expect(history).toHaveLength(10);
        expect(history[0].id).toBe("health-6");
        expect(history[9].id).toBe("health-15");
    });

    it("sets the latest reading from history", async () => {
        mockedNodesApi.getHealthHistory.mockResolvedValue([
            health1,
            health2,
            health3,
        ]);

        const store = createStore();

        await store.dispatch(
            fetchHealthHistory({
                nodeId: "node-1",
            }),
        );

        expect(
            store.getState().health.latestByNodeId["node-1"],
        ).toEqual(health3);
    });

    it("sets loading while fetching history", () => {
        const state = healthReducer(
            undefined,
            fetchHealthHistory.pending(
                "request-1",
                {
                    nodeId: "node-1",
                    limit: 10,
                },
            ),
        );

        expect(
            state.loadingByNodeId["node-1"],
        ).toBe(true);

        expect(
            state.errorByNodeId["node-1"],
        ).toBeNull();
    });

    it("stores an error when history fetch fails", async () => {
        mockedNodesApi.getHealthHistory.mockRejectedValue(
            new Error("Network error"),
        );

        const store = createStore();

        await store.dispatch(
            fetchHealthHistory({
                nodeId: "node-1",
            }),
        );

        expect(
            store.getState().health.errorByNodeId["node-1"],
        ).toBe("Network error");

        expect(
            store.getState().health.loadingByNodeId["node-1"],
        ).toBe(false);
    });

    it("fetches and stores latest health", async () => {
        mockedNodesApi.getLatestHealth.mockResolvedValue(
            health3,
        );

        const store = createStore();

        await store.dispatch(
            fetchLatestHealth("node-1"),
        );

        expect(
            store.getState().health.latestByNodeId["node-1"],
        ).toEqual(health3);
    });

    it("stores null when latest health does not exist", async () => {
        mockedNodesApi.getLatestHealth.mockResolvedValue(
            null,
        );

        const store = createStore();

        await store.dispatch(
            fetchLatestHealth("node-1"),
        );

        expect(
            store.getState().health.latestByNodeId["node-1"],
        ).toBeNull();
    });

    it("merges new latest health into existing history", async () => {
        const latest: HealthCheck = {
            id: "health-4",
            computeNodeId: "node-1",
            isHealthy: true,
            cpuUsagePercent: 68,
            gpuUsagePercent: 73,
            gpuTemperatureCelsius: 75,
            checkedAt: "2026-10-06T16:20:40.000Z",
        };

        mockedNodesApi.getHealthHistory.mockResolvedValue([
            health1,
            health2,
            health3,
        ]);

        mockedNodesApi.getLatestHealth.mockResolvedValue(
            latest,
        );

        const store = createStore();

        await store.dispatch(
            fetchHealthHistory({
                nodeId: "node-1",
            }),
        );

        await store.dispatch(
            fetchLatestHealth("node-1"),
        );

        const history =
            store.getState().health.historyByNodeId["node-1"];

        expect(history).toHaveLength(4);
        expect(history[3]).toEqual(latest);
    });

    it("replaces an existing history item when latest health has the same id", async () => {
        const updatedHealth2: HealthCheck = {
            ...health2,
            cpuUsagePercent: 69,
            gpuTemperatureCelsius: 73,
        };

        mockedNodesApi.getHealthHistory.mockResolvedValue([
            health1,
            health2,
            health3,
        ]);

        mockedNodesApi.getLatestHealth.mockResolvedValue(
            updatedHealth2,
        );

        const store = createStore();

        await store.dispatch(
            fetchHealthHistory({
                nodeId: "node-1",
            }),
        );

        await store.dispatch(
            fetchLatestHealth("node-1"),
        );

        const history =
            store.getState().health.historyByNodeId["node-1"];

        expect(history).toHaveLength(3);
        expect(history[1]).toEqual(updatedHealth2);
    });
    
it("keeps history sorted after merging latest health", async () => {
  mockedNodesApi.getHealthHistory.mockResolvedValue([
    health1,
    health2,
    health3,
  ]);

  const store = createStore();
  await store.dispatch(
    fetchHealthHistory({ nodeId: "node-1" }),
  );

  const updatedHealth2 = {
    ...health2,
    checkedAt: "2026-10-06T16:19:50.000Z",
  };

  mockedNodesApi.getLatestHealth.mockResolvedValue(
    updatedHealth2,
  );

  await store.dispatch(
    fetchLatestHealth("node-1"),
  );

  const history =
    store.getState().health.historyByNodeId["node-1"];

  expect(history.map((item) => item.id)).toEqual([
    "health-2",
    "health-1",
    "health-3",
  ]);

  expect(history.map((item) => item.checkedAt)).toEqual([
    "2026-10-06T16:19:50.000Z",
    "2026-10-06T16:20:00.000Z",
    "2026-10-06T16:20:30.000Z",
  ]);
});

    it("adds latest health to an empty history", async () => {
        mockedNodesApi.getLatestHealth.mockResolvedValue(
            health1,
        );

        const store = createStore();

        await store.dispatch(
            fetchLatestHealth("node-1"),
        );

        expect(
            store.getState().health.historyByNodeId["node-1"],
        ).toEqual([health1]);
    });

    it("sets an error when latest health fetch fails", async () => {
        mockedNodesApi.getLatestHealth.mockRejectedValue(
            new Error("Latest health failed"),
        );

        const store = createStore();

        await store.dispatch(
            fetchLatestHealth("node-1"),
        );

        expect(
            store.getState().health.errorByNodeId["node-1"],
        ).toBe("Latest health failed");

        expect(
            store.getState().health.loadingByNodeId["node-1"],
        ).toBe(false);
    });

    it("clears health for one node", () => {
        const state = {
            historyByNodeId: {
                "node-1": [health1],
                "node-2": [health2],
            },
            latestByNodeId: {
                "node-1": health1,
                "node-2": health2,
            },
            loadingByNodeId: {
                "node-1": true,
                "node-2": false,
            },
            errorByNodeId: {
                "node-1": "error",
                "node-2": null,
            },
        };

        const nextState = healthReducer(
            state,
            clearNodeHealth("node-1"),
        );

        expect(
            nextState.historyByNodeId["node-1"],
        ).toBeUndefined();

        expect(
            nextState.latestByNodeId["node-1"],
        ).toBeUndefined();

        expect(
            nextState.loadingByNodeId["node-1"],
        ).toBeUndefined();

        expect(
            nextState.errorByNodeId["node-1"],
        ).toBeUndefined();

        expect(
            nextState.historyByNodeId["node-2"],
        ).toEqual([health2]);
    });

    it("clears all health state", () => {
        const state = {
            historyByNodeId: {
                "node-1": [health1],
            },
            latestByNodeId: {
                "node-1": health1,
            },
            loadingByNodeId: {
                "node-1": true,
            },
            errorByNodeId: {
                "node-1": "error",
            },
        };

        const nextState = healthReducer(
            state,
            clearAllHealth(),
        );

        expect(nextState).toEqual({
            historyByNodeId: {},
            latestByNodeId: {},
            loadingByNodeId: {},
            errorByNodeId: {},
        });
    });
});