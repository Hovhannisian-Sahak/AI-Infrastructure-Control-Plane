import {
    createAsyncThunk,
    createSlice,
} from "@reduxjs/toolkit";
import type {
    Incident,
    IncidentSeverity,
    IncidentStatus,
} from "@/lib/api/models/incident";
import { incidentsApi } from "@/lib/api/incidentsApi";
import type { PageResponse } from "@/lib/api/models/pageResponse";

type IncidentsState = {
    incidents: Incident[];
    page: number;
    pageSize: number;
    totalCount: number;
    loading: boolean;
    error: string | null;
    requestId: string | null;
};

const initialState: IncidentsState = {
    incidents: [],
    page: 1,
    pageSize: 12,
    totalCount: 0,
    loading: false,
    error: null,
    requestId: null,
};

type IncidentQuery = {
    page: number;
    pageSize: number;
    severity?: IncidentSeverity;
    status?: IncidentStatus;
    computeNodeId?: string;
    from?: string;
    to?: string;
};

export const fetchIncidents = createAsyncThunk<
    PageResponse<Incident>,
    IncidentQuery,
    { rejectValue: string }
>(
    "incidents/fetchIncidents",
    async (params, { rejectWithValue }) => {
        try {
            return await incidentsApi.search(params);
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Unable to load incidents. Please try again.",
            );
        }
    },
);

const incidentsSlice = createSlice({
    name: "incidents",
    initialState,
    reducers: {
        clearIncidentError: (state) => {
            state.error = null;
        },
    },
    extraReducers: (builder) => {
        builder
            .addCase(fetchIncidents.pending, (state, action) => {
                state.loading = true;
                state.error = null;
                state.requestId = action.meta.requestId;
            })
            .addCase(fetchIncidents.fulfilled, (state, action) => {
                if (state.requestId !== action.meta.requestId) return;
                state.loading = false;
                state.incidents = action.payload.items;
                state.page = action.payload.page;
                state.pageSize = action.payload.pageSize;
                state.totalCount = action.payload.totalCount;
                state.requestId = null;
            })
            .addCase(fetchIncidents.rejected, (state, action) => {
                if (state.requestId !== action.meta.requestId) return;
                state.loading = false;
                state.error =
                    action.payload ||
                    action.error.message ||
                    "Unable to load incidents. Please try again.";
                state.requestId = null;
            });
    },
});

export const {
    clearIncidentError,
} = incidentsSlice.actions;

export default incidentsSlice.reducer;