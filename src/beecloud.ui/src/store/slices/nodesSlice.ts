import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { nodesApi } from "@/lib/api/nodesApi";
import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { CreateComputeNodeRequest } from "@/lib/api/models/createComputeNodeRequest";
type NodesState = {
  nodes: ComputeNode[];
  loading: boolean;
  creating: boolean;
  refreshing: boolean;
  createSuccess: string | null;
  actionLoadingByNodeId: Record<string, boolean>;
  error: string | null;
};

const initialState: NodesState = {
  nodes: [],
  loading: false,
  creating: false,
  refreshing: false,
  createSuccess: null,
  actionLoadingByNodeId: {},
  error: null,
};

export const fetchNodes = createAsyncThunk("nodes/fetchNodes", async () => {
  return nodesApi.getAll();
});
export const createNode = createAsyncThunk(
  "nodes/createNode",
  async (request: CreateComputeNodeRequest) => {
    return nodesApi.create(request);
  },
);
export const startNode = createAsyncThunk("nodes/startNode", async (id: string) => {
  return nodesApi.start(id);
});

export const stopNode = createAsyncThunk("nodes/stopNode", async (id: string) => {
  return nodesApi.stop(id);
});

export const restartNode = createAsyncThunk("nodes/restartNode", async (id: string) => {
  return nodesApi.restart(id);
});

const nodesSlice = createSlice({
  name: "nodes",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchNodes.pending, (state) => {
        if (state.nodes.length === 0) {
          state.loading = true;
        } else {
          state.refreshing = true;
        }

        state.error = null;
      })
      .addCase(fetchNodes.fulfilled, (state, action) => {
        state.loading = false;
        state.refreshing = false;
        state.nodes = action.payload;
      })
      .addCase(fetchNodes.rejected, (state, action) => {
        state.loading = false;
        state.refreshing = false;
        state.error = action.error.message ?? "Failed to load nodes";
      })
      .addCase(createNode.pending, (state) => {
        state.creating = true;
        state.createSuccess = null;
        state.error = null;
      })
      .addCase(createNode.fulfilled, (state, action) => {
        state.creating = false;
        state.createSuccess = "Node created successfully.";
        state.nodes.push(action.payload);
      })
      .addCase(createNode.rejected, (state, action) => {
        state.creating = false;
        state.createSuccess = null;
        state.error = action.error.message ?? "Failed to create node";
      })
      .addCase(startNode.pending, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = true;
        state.error = null;
      })
      .addCase(startNode.fulfilled, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = false;

        const index = state.nodes.findIndex((node) => node.id === action.payload.id);

        if (index !== -1) {
          state.nodes[index] = action.payload;
        }
      })
      .addCase(startNode.rejected, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = false;
        state.error = action.error.message ?? "Failed to start node";
      })
      .addCase(stopNode.pending, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = true;
        state.error = null;
      })
      .addCase(stopNode.fulfilled, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = false;

        const index = state.nodes.findIndex((node) => node.id === action.payload.id);

        if (index !== -1) {
          state.nodes[index] = action.payload;
        }
      })
      .addCase(stopNode.rejected, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = false;
        state.error = action.error.message ?? "Failed to stop node";
      })
      .addCase(restartNode.pending, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = true;
        state.error = null;
      })
      .addCase(restartNode.fulfilled, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = false;

        const index = state.nodes.findIndex((node) => node.id === action.payload.id);

        if (index !== -1) {
          state.nodes[index] = action.payload;
        }
      })
      .addCase(restartNode.rejected, (state, action) => {
        state.actionLoadingByNodeId[action.meta.arg] = false;
        state.error = action.error.message ?? "Failed to restart node";
      });
  },
});

export default nodesSlice.reducer;
