import {
    createAsyncThunk,
    createSlice,
    type PayloadAction,
} from "@reduxjs/toolkit";

import { nodesApi } from "@/lib/api/nodesApi";
import type { HealthCheck } from "@/lib/api/models/healthCheck";

type HealthState = {
    historyByNodeId: Record<string, HealthCheck[]>;
    latestByNodeId: Record<string, HealthCheck | null>;
    loadingByNodeId: Record<string, boolean>;
    errorByNodeId: Record<string, string | null>;
};

const initialState: HealthState = {
    historyByNodeId: {},
    latestByNodeId: {},
    loadingByNodeId: {},
    errorByNodeId: {},
};

export const fetchHealthHistory = createAsyncThunk<
    {
        nodeId: string;
        history: HealthCheck[];
    },
    {
        nodeId: string;
        limit?: number;
        from?: string;
        to?: string;
    },
    {
        rejectValue: string;
    }
>(
    "health/fetchHealthHistory",
    async (
        { nodeId, limit = 10, from, to },
        { rejectWithValue },
    ) => {
        try {
            const history = await nodesApi.getHealthHistory(
                nodeId,
                limit,
                from,
                to,
            );

            const sortedHistory = [...history].sort(
                (a, b) =>
                    new Date(a.checkedAt).getTime() -
                    new Date(b.checkedAt).getTime(),
            );

            return {
                nodeId,
                history: sortedHistory,
            };
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Failed to fetch health history.",
            );
        }
    },
);

export const fetchLatestHealth = createAsyncThunk<
    {
        nodeId: string;
        health: HealthCheck | null;
    },
    string,
    {
        rejectValue: string;
    }
>(
    "health/fetchLatestHealth",
    async (nodeId, { rejectWithValue }) => {
        try {
            const health = await nodesApi.getLatestHealth(nodeId);

            return {
                nodeId,
                health,
            };
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Failed to fetch latest health.",
            );
        }
    },
);

const healthSlice = createSlice({
    name: "health",
    initialState,
    reducers: {
        clearNodeHealth: (
            state,
            action: PayloadAction<string>,
        ) => {
            const nodeId = action.payload;

            delete state.historyByNodeId[nodeId];
            delete state.latestByNodeId[nodeId];
            delete state.loadingByNodeId[nodeId];
            delete state.errorByNodeId[nodeId];
        },

        clearAllHealth: (state) => {
            state.historyByNodeId = {};
            state.latestByNodeId = {};
            state.loadingByNodeId = {};
            state.errorByNodeId = {};
        },
    },

    extraReducers: (builder) => {
        builder
            .addCase(
                fetchHealthHistory.pending,
                (state, action) => {
                    const nodeId = action.meta.arg.nodeId;

                    state.loadingByNodeId[nodeId] = true;
                    state.errorByNodeId[nodeId] = null;
                },
            )
            .addCase(
                fetchHealthHistory.fulfilled,
                (state, action) => {
                    const {
                        nodeId,
                        history,
                    } = action.payload;

                    state.loadingByNodeId[nodeId] = false;
                    state.historyByNodeId[nodeId] = history;

                    state.latestByNodeId[nodeId] =
                        history.length > 0
                            ? history[history.length - 1]
                            : null;
                },
            )
            .addCase(
                fetchHealthHistory.rejected,
                (state, action) => {
                    const nodeId = action.meta.arg.nodeId;

                    state.loadingByNodeId[nodeId] = false;
                    state.errorByNodeId[nodeId] =
                        action.payload ??
                        action.error.message ??
                        "Failed to fetch health history.";
                },
            )
            .addCase(
                fetchLatestHealth.pending,
                (state, action) => {
                    const nodeId = action.meta.arg;

                    state.loadingByNodeId[nodeId] = true;
                    state.errorByNodeId[nodeId] = null;
                },
            )
            .addCase(
                fetchLatestHealth.fulfilled,
                (state, action) => {
                    const {
                        nodeId,
                        health,
                    } = action.payload;

                    state.loadingByNodeId[nodeId] = false;
                    state.latestByNodeId[nodeId] = health;

                    if (health) {
                        const history =
                            state.historyByNodeId[nodeId] ?? [];

                        const withoutCurrent =
                            history.filter(
                                (item) => item.id !== health.id,
                            );

                        state.historyByNodeId[nodeId] = [
                            ...withoutCurrent,
                            health,
                        ].sort(
                            (a, b) =>
                                new Date(a.checkedAt).getTime() -
                                new Date(b.checkedAt).getTime(),
                        );
                    }
                },
            )
            .addCase(
                fetchLatestHealth.rejected,
                (state, action) => {
                    const nodeId = action.meta.arg;

                    state.loadingByNodeId[nodeId] = false;
                    state.errorByNodeId[nodeId] =
                        action.payload ??
                        action.error.message ??
                        "Failed to fetch latest health.";
                },
            );
    },
});

export const {
    clearNodeHealth,
    clearAllHealth,
} = healthSlice.actions;

export default healthSlice.reducer;