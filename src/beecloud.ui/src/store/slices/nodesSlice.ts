import {
  createAsyncThunk,
  createSlice,
} from "@reduxjs/toolkit";
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
    deletingNodeId: string | null;
    deleteErrorByNodeId: Record<string, string | null>;
    error: string | null;
};

const initialState: NodesState = {
  nodes: [],
  loading: false,
  creating: false,
  refreshing: false,
  createSuccess: null,
  actionLoadingByNodeId: {},
  deletingNodeId: null,
  deleteErrorByNodeId: {},
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
    async (
        request: CreateComputeNodeRequest,
    ) => {
      return nodesApi.create(request);
    },
);

export const deleteNode = createAsyncThunk(
    "nodes/deleteNode",
    async (id: string) => {
        await nodesApi.delete(id);
        return id;
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

export const restartNode = createAsyncThunk(
    "nodes/restartNode",
    async (id: string) => {
      return nodesApi.restart(id);
    },
);

const nodesSlice = createSlice({
  name: "nodes",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder

        // -----------------------------
        // Fetch nodes
        // -----------------------------

        .addCase(
            fetchNodes.pending,
            (state) => {
              if (state.nodes.length === 0) {
                state.loading = true;
              } else {
                state.refreshing = true;
              }

              state.error = null;
            },
        )

        .addCase(
            fetchNodes.fulfilled,
            (state, action) => {
              state.loading = false;
              state.refreshing = false;
              state.nodes = action.payload;
              state.error = null;
            },
        )

        .addCase(
            fetchNodes.rejected,
            (state, action) => {
              state.loading = false;
              state.refreshing = false;

              state.error =
                  action.error.message ??
                  "Unable to load nodes. Please try again.";
            },
        )

        // -----------------------------
        // Create node
        // -----------------------------

        .addCase(
            createNode.pending,
            (state) => {
              state.creating = true;
              state.createSuccess = null;
              state.error = null;
            },
        )

        .addCase(
            createNode.fulfilled,
            (state, action) => {
              state.creating = false;
              state.createSuccess =
                  "Node created successfully.";
              state.nodes.push(action.payload);
              state.error = null;
            },
        )

        .addCase(
            createNode.rejected,
            (state, action) => {
              state.creating = false;
              state.createSuccess = null;

              const error =
                  action.error.message ??
                  "Unable to create the node. Please try again.";

              state.error =
                  error.includes(
                      "conflicts with the current state",
                  )
                      ? "A node with this name already exists."
                      : error;
            },
        )
        // -----------------------------
        // Delete node
        // -----------------------------

        .addCase(
            deleteNode.pending,
            (state, action) => {
                const nodeId = action.meta.arg;

                state.deletingNodeId = nodeId;
                state.deleteErrorByNodeId[nodeId] = null;
            },
        )

        .addCase(
            deleteNode.fulfilled,
            (state, action) => {
                const nodeId = action.payload;

                state.nodes = state.nodes.filter(
                    (node) => node.id !== nodeId,
                );

                delete state.deleteErrorByNodeId[nodeId];

                state.deletingNodeId = null;
            },
        )

        .addCase(
            deleteNode.rejected,
            (state, action) => {
                const nodeId = action.meta.arg;

                state.deletingNodeId = null;

                state.deleteErrorByNodeId[nodeId] =
                    action.error.message ??
                    "Unable to delete the node. Please try again.";
            },
        )
        // -----------------------------
        // Start node
        // -----------------------------

        .addCase(
            startNode.pending,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = true;
              state.error = null;
            },
        )

        .addCase(
            startNode.fulfilled,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = false;

              const index =
                  state.nodes.findIndex(
                      (node) =>
                          node.id ===
                          action.payload.id,
                  );

              if (index !== -1) {
                state.nodes[index] =
                    action.payload;
              }

              state.error = null;
            },
        )

        .addCase(
            startNode.rejected,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = false;

              state.error =
                  action.error.message ??
                  "Unable to start the node. Please try again.";
            },
        )

        // -----------------------------
        // Stop node
        // -----------------------------

        .addCase(
            stopNode.pending,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = true;
              state.error = null;
            },
        )

        .addCase(
            stopNode.fulfilled,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = false;

              const index =
                  state.nodes.findIndex(
                      (node) =>
                          node.id ===
                          action.payload.id,
                  );

              if (index !== -1) {
                state.nodes[index] =
                    action.payload;
              }

              state.error = null;
            },
        )

        .addCase(
            stopNode.rejected,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = false;

              state.error =
                  action.error.message ??
                  "Unable to stop the node. Please try again.";
            },
        )

        // -----------------------------
        // Restart node
        // -----------------------------

        .addCase(
            restartNode.pending,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = true;
              state.error = null;
            },
        )

        .addCase(
            restartNode.fulfilled,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = false;

              const index =
                  state.nodes.findIndex(
                      (node) =>
                          node.id ===
                          action.payload.id,
                  );

              if (index !== -1) {
                state.nodes[index] =
                    action.payload;
              }

              state.error = null;
            },
        )

        .addCase(
            restartNode.rejected,
            (state, action) => {
              state.actionLoadingByNodeId[
                  action.meta.arg
                  ] = false;

              state.error =
                  action.error.message ??
                  "Unable to restart the node. Please try again.";
            },
        );
  },
});

export default nodesSlice.reducer;