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

type IncidentsState = {
    incidents: Incident[];
    loading: boolean;
    error: string | null;
};

const initialState: IncidentsState = {
    incidents: [],
    loading: false,
    error: null,
};

export const fetchIncidents = createAsyncThunk(
    "incidents/fetchIncidents",
    async (
        params?: {
            severity?: IncidentSeverity;
            status?: IncidentStatus;
        },
    ) => {
        return await incidentsApi.getAll(params);
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
            .addCase(fetchIncidents.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(fetchIncidents.fulfilled, (state, action) => {
                state.loading = false;
                state.incidents = action.payload;
            })
            .addCase(fetchIncidents.rejected, (state, action) => {
                state.loading = false;
                state.error =
                    action.error.message ||
                    "Unable to load incidents. Please try again.";
            });
    },
});

export const {
    clearIncidentError,
} = incidentsSlice.actions;

export default incidentsSlice.reducer;