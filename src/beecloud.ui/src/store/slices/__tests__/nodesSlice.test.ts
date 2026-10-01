import { configureStore } from "@reduxjs/toolkit";
import nodesReducer, { fetchNodes } from "../nodesSlice";
import { nodesApi } from "@/lib/api/nodesApi";

jest.mock("@/lib/api/nodesApi");

const mockedNodesApi = jest.mocked(nodesApi);

describe("nodesSlice", () => {
    it("stores nodes when fetchNodes succeeds", async () => {
        const nodes = [
            {
                id: "node-1",
                name: "GPU Node 1",
                gpuModel: "NVIDIA A100",
                gpuCount: 4,
                status: "Available" as const,
                activeFault: "None" as const,
            },
        ];

        mockedNodesApi.getAll.mockResolvedValue(nodes);

        const store = configureStore({
            reducer: {
                nodes: nodesReducer,
            },
        });

        await store.dispatch(fetchNodes());

        const state = store.getState().nodes;

        expect(state.loading).toBe(false);
        expect(state.error).toBeNull();
        expect(state.nodes).toEqual(nodes);
    });

    it("stores an error when fetchNodes fails", async () => {
        mockedNodesApi.getAll.mockRejectedValue(
            new Error("API unavailable"),
        );

        const store = configureStore({
            reducer: {
                nodes: nodesReducer,
            },
        });

        await store.dispatch(fetchNodes());

        const state = store.getState().nodes;

        expect(state.loading).toBe(false);
        expect(state.error).toBe("API unavailable");
        expect(state.nodes).toEqual([]);
    });
});