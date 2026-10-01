import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { nodesApi } from "@/lib/api/nodesApi";
import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { CreateComputeNodeRequest } from "@/lib/api/models/createComputeNodeRequest";
type NodesState = {
    nodes: ComputeNode[];
    loading: boolean;
    actionLoadingByNodeId: Record<string, boolean>;
    error: string | null;
};

const initialState: NodesState = {
    nodes: [],
    loading: false,
    actionLoadingByNodeId: {},
    error: null,
};

export const fetchNodes = createAsyncThunk(
    "nodes/fetchNodes",
    async () => {
        return nodesApi.getAll();
    },
);
export const createNode = createAsyncThunk(
    "nodes/createNode",
    async (request: CreateComputeNodeRequest) => {
        return nodesApi.create(request);
    },
);
export const startNode = createAsyncThunk(
    "nodes/startNode",
    async (id: string) => {
        return nodesApi.start(id);
    },
);

export const stopNode = createAsyncThunk(
    "nodes/stopNode",
    async (id: string) => {
        return nodesApi.stop(id);
    },
);

const nodesSlice = createSlice({
    name: "nodes",
    initialState,
    reducers: {},
    extraReducers: (builder) => {
        builder
            .addCase(fetchNodes.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(fetchNodes.fulfilled, (state, action) => {
                state.loading = false;
                state.nodes = action.payload;
            })
            .addCase(fetchNodes.rejected, (state, action) => {
                state.loading = false;
                state.error =
                    action.error.message ?? "Failed to load nodes";
            })
            .addCase(createNode.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(createNode.fulfilled, (state, action) => {
                state.loading = false;
                state.nodes.push(action.payload);
            })
            .addCase(createNode.rejected, (state, action) => {
                state.loading = false;
                state.error =
                    action.error.message ?? "Failed to create node";
            })
            .addCase(startNode.pending, (state, action) => {
                state.actionLoadingByNodeId[action.meta.arg] = true;
                state.error = null;
            })
            .addCase(startNode.fulfilled, (state, action) => {
                state.actionLoadingByNodeId[action.meta.arg] = false;

                const index = state.nodes.findIndex(
                    (node) => node.id === action.payload.id,
                );

                if (index !== -1) {
                    state.nodes[index] = action.payload;
                }
            })
            .addCase(startNode.rejected, (state, action) => {
                state.actionLoadingByNodeId[action.meta.arg] = false;
                state.error =
                    action.error.message ?? "Failed to start node";
            })
            .addCase(stopNode.pending, (state, action) => {
                state.actionLoadingByNodeId[action.meta.arg] = true;
                state.error = null;
            })
            .addCase(stopNode.fulfilled, (state, action) => {
                state.actionLoadingByNodeId[action.meta.arg] = false;

                const index = state.nodes.findIndex(
                    (node) => node.id === action.payload.id,
                );

                if (index !== -1) {
                    state.nodes[index] = action.payload;
                }
            })
            .addCase(stopNode.rejected, (state, action) => {
                state.actionLoadingByNodeId[action.meta.arg] = false;
                state.error =
                    action.error.message ?? "Failed to stop node";
            });
    },
});

export default nodesSlice.reducer;