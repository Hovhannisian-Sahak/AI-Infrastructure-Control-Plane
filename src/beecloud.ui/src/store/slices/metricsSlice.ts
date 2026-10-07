import {
    createAsyncThunk,
    createSlice,
} from "@reduxjs/toolkit";

import { nodesApi } from "@/lib/api/nodesApi";
import type { NodeMetric } from "@/lib/api/models/nodeMetric";

type MetricsState = {
    historyByNodeId: Record<string, NodeMetric[]>;
    loadingByNodeId: Record<string, boolean>;
    errorByNodeId: Record<string, string | null>;
    requestIdByNodeId: Record<string, string>;
};

const initialState: MetricsState = {
    historyByNodeId: {},
    loadingByNodeId: {},
    errorByNodeId: {},
    requestIdByNodeId: {},
};

export const fetchNodeMetrics = createAsyncThunk<
    { nodeId: string; history: NodeMetric[] },
    { nodeId: string; limit?: number; from?: string; to?: string },
    { rejectValue: string }
>(
    "metrics/fetchNodeMetrics",
    async (
        { nodeId, limit = 100, from, to },
        { rejectWithValue },
    ) => {
        try {
            const history = await nodesApi.getNodeMetrics(
                nodeId,
                limit,
                from,
                to,
            );

            return {
                nodeId,
                history: [...history].sort(
                    (left, right) =>
                        new Date(left.recordedAt).getTime() -
                        new Date(right.recordedAt).getTime(),
                ),
            };
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Failed to fetch node metrics.",
            );
        }
    },
);

const metricsSlice = createSlice({
    name: "metrics",
    initialState,
    reducers: {},
    extraReducers: builder => {
        builder
            .addCase(fetchNodeMetrics.pending, (state, action) => {
                const { nodeId } = action.meta.arg;
                state.requestIdByNodeId[nodeId] = action.meta.requestId;
                state.loadingByNodeId[nodeId] = true;
                state.errorByNodeId[nodeId] = null;
            })
            .addCase(fetchNodeMetrics.fulfilled, (state, action) => {
                const { nodeId, history } = action.payload;
                if (state.requestIdByNodeId[nodeId] !== action.meta.requestId) {
                    return;
                }

                state.loadingByNodeId[nodeId] = false;
                state.historyByNodeId[nodeId] = history;
                delete state.requestIdByNodeId[nodeId];
            })
            .addCase(fetchNodeMetrics.rejected, (state, action) => {
                const { nodeId } = action.meta.arg;
                if (state.requestIdByNodeId[nodeId] !== action.meta.requestId) {
                    return;
                }

                state.loadingByNodeId[nodeId] = false;
                state.errorByNodeId[nodeId] =
                    action.payload ??
                    action.error.message ??
                    "Failed to fetch node metrics.";
                delete state.requestIdByNodeId[nodeId];
            });
    },
});

export default metricsSlice.reducer;
