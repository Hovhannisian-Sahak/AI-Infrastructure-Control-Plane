import { configureStore } from "@reduxjs/toolkit";
import nodesReducer, {
    fetchNodes,
    createNode,
    startNode,
    stopNode,
} from "../nodesSlice";
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
    it("adds a node when createNode succeeds", async () => {
        const request = {
            name: "GPU Node 2",
            gpuModel: "NVIDIA H100",
            gpuCount: 8,
        };

        const createdNode = {
            id: "node-2",
            name: "GPU Node 2",
            gpuModel: "NVIDIA H100",
            gpuCount: 8,
            status: "Provisioning" as const,
            activeFault: "None" as const,
        };

        mockedNodesApi.create.mockResolvedValue(createdNode);

        const store = configureStore({
            reducer: {
                nodes: nodesReducer,
            },
        });

        await store.dispatch(createNode(request));

        const state = store.getState().nodes;

        expect(state.loading).toBe(false);
        expect(state.error).toBeNull();
        expect(state.nodes).toEqual([createdNode]);
    });

    it("stores an error when createNode fails", async () => {
        mockedNodesApi.create.mockRejectedValue(
            new Error("Node name already exists"),
        );

        const store = configureStore({
            reducer: {
                nodes: nodesReducer,
            },
        });

        await store.dispatch(
            createNode({
                name: "GPU Node 2",
                gpuModel: "NVIDIA H100",
                gpuCount: 8,
            }),
        );

        const state = store.getState().nodes;

        expect(state.loading).toBe(false);
        expect(state.error).toBe("Node name already exists");
        expect(state.nodes).toEqual([]);
    });
    it("tracks action loading state when starting a node", async () => {
        const node = {
            id: "node-1",
            name: "GPU Node 1",
            gpuModel: "NVIDIA A100",
            gpuCount: 4,
            status: "Available" as const,
            activeFault: "None" as const,
        };

        mockedNodesApi.start.mockResolvedValue({
            ...node,
            status: "Running",
        });

        const store = configureStore({
            reducer: {
                nodes: nodesReducer,
            },
        });

        store.dispatch({
            type: "nodes/fetchNodes/fulfilled",
            payload: [node],
        });
        
        const promise = store.dispatch(startNode(node.id));

        expect(
            store.getState().nodes.actionLoadingByNodeId["node-1"],
        ).toBe(true);

        await promise;

        const state = store.getState().nodes;

        expect(
            state.actionLoadingByNodeId["node-1"],
        ).toBe(false);

        expect(state.nodes).toEqual([
            {
                ...node,
                status: "Running",
            },
        ]);
    });
    it("tracks action loading state when stopping a node", async () => {
        const node = {
            id: "node-1",
            name: "GPU Node 1",
            gpuModel: "NVIDIA A100",
            gpuCount: 4,
            status: "Running" as const,
            activeFault: "None" as const,
        };

        mockedNodesApi.stop.mockResolvedValue({
            ...node,
            status: "Stopped",
        });

        const store = configureStore({
            reducer: {
                nodes: nodesReducer,
            },
        });

        store.dispatch({
            type: "nodes/fetchNodes/fulfilled",
            payload: [node],
        });
        
        const promise = store.dispatch(stopNode(node.id));

        expect(
            store.getState().nodes.actionLoadingByNodeId["node-1"],
        ).toBe(true);

        await promise;

        const state = store.getState().nodes;

        expect(
            state.actionLoadingByNodeId["node-1"],
        ).toBe(false);

        expect(state.nodes).toEqual([
            {
                ...node,
                status: "Stopped",
            },
        ]);
    });
});