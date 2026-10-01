import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { nodesApi } from "@/lib/api/nodesApi";
import type { ComputeNode } from "@/lib/api/models/computeNode";

type NodesState = {
    nodes: ComputeNode[];
    loading: boolean;
    error: string | null;
};

const initialState: NodesState = {
    nodes: [],
    loading: false,
    error: null,
};

export const fetchNodes = createAsyncThunk(
    "nodes/fetchNodes",
    async () => {
        return nodesApi.getAll();
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
                state.error = action.error.message ?? "Failed to load nodes";
            });
    },
});

export default nodesSlice.reducer;