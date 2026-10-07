import { configureStore } from "@reduxjs/toolkit";
import metricsReducer, { fetchNodeMetrics } from "../metricsSlice";
import { nodesApi } from "@/lib/api/nodesApi";
import type { NodeMetric } from "@/lib/api/models/nodeMetric";

jest.mock("@/lib/api/nodesApi");

const mockedNodesApi = jest.mocked(nodesApi);
const metric = (id: string, recordedAt: string): NodeMetric => ({
    id,
    computeNodeId: "node-1",
    cpuUsagePercent: 40,
    gpuUsagePercent: 50,
    gpuTemperatureCelsius: 60,
    recordedAt,
});

describe("metricsSlice", () => {
    beforeEach(() => jest.clearAllMocks());

    it("loads filtered metrics and stores them in chronological order", async () => {
        const from = "2026-10-07T10:00:00.000Z";
        const to = "2026-10-07T14:00:00.000Z";
        const oldMetric = metric("old", "2026-10-07T10:00:00.000Z");
        const newMetric = metric("new", "2026-10-07T11:00:00.000Z");
        mockedNodesApi.getNodeMetrics.mockResolvedValue([newMetric, oldMetric]);
        const store = configureStore({
            reducer: { metrics: metricsReducer },
        });

        await store.dispatch(fetchNodeMetrics({
            nodeId: "node-1",
            limit: 100,
            from,
            to,
        }));

        expect(mockedNodesApi.getNodeMetrics).toHaveBeenCalledWith(
            "node-1",
            100,
            from,
            to,
        );
        expect(store.getState().metrics.historyByNodeId["node-1"]).toEqual([
            oldMetric,
            newMetric,
        ]);
    });

    it("records fetch errors", async () => {
        mockedNodesApi.getNodeMetrics.mockRejectedValue(new Error("Metrics unavailable"));
        const store = configureStore({
            reducer: { metrics: metricsReducer },
        });

        await store.dispatch(fetchNodeMetrics({ nodeId: "node-1" }));

        expect(store.getState().metrics.errorByNodeId["node-1"])
            .toBe("Metrics unavailable");
        expect(store.getState().metrics.loadingByNodeId["node-1"]).toBe(false);
    });

    it("ignores an older range response that arrives after the latest request", async () => {
        let resolveFirst: (value: NodeMetric[]) => void = () => {};
        const firstResponse = new Promise<NodeMetric[]>(resolve => {
            resolveFirst = resolve;
        });
        const earlier = metric("earlier-range", "2026-10-07T09:00:00.000Z");
        const latest = metric("latest-range", "2026-10-07T13:00:00.000Z");
        mockedNodesApi.getNodeMetrics
            .mockReturnValueOnce(firstResponse)
            .mockResolvedValueOnce([latest]);

        const store = configureStore({
            reducer: { metrics: metricsReducer },
        });

        const firstRequest = store.dispatch(fetchNodeMetrics({
            nodeId: "node-1",
            from: "2026-10-07T08:00:00.000Z",
            to: "2026-10-07T10:00:00.000Z",
        }));
        await store.dispatch(fetchNodeMetrics({
            nodeId: "node-1",
            from: "2026-10-07T12:00:00.000Z",
            to: "2026-10-07T14:00:00.000Z",
        }));
        resolveFirst([earlier]);
        await firstRequest;

        expect(store.getState().metrics.historyByNodeId["node-1"])
            .toEqual([latest]);
    });
});
