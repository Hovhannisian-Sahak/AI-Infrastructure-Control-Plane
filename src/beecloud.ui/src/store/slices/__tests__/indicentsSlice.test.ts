import { configureStore } from "@reduxjs/toolkit";
import incidentsReducer, {
    fetchIncidents,
    clearIncidentError,
} from "../incidentsSlice";
import { incidentsApi } from "@/lib/api/incidentsApi";

jest.mock("@/lib/api/incidentsApi");

const mockedIncidentsApi = jest.mocked(incidentsApi);

describe("incidentsSlice", () => {
    it("stores incidents when fetchIncidents succeeds", async () => {
        const incidents = [
            {
                id: "incident-1",
                computeNodeId: "node-1",
                title: "GPU failure",
                description: "GPU failure detected.",
                severity: "Critical" as const,
                status: "Open" as const,
                createdAt: "2026-10-05T10:00:00Z",
            },
        ];

        mockedIncidentsApi.getAll.mockResolvedValue(incidents);

        const store = configureStore({
            reducer: {
                incidents: incidentsReducer,
            },
        });

        await store.dispatch(fetchIncidents());

        const state = store.getState().incidents;

        expect(state.loading).toBe(false);
        expect(state.error).toBeNull();
        expect(state.incidents).toEqual(incidents);
    });

    it("stores an error when fetchIncidents fails", async () => {
        mockedIncidentsApi.getAll.mockRejectedValue(
            new Error("API unavailable"),
        );

        const store = configureStore({
            reducer: {
                incidents: incidentsReducer,
            },
        });

        await store.dispatch(fetchIncidents());

        const state = store.getState().incidents;

        expect(state.loading).toBe(false);
        expect(state.error).toBe("API unavailable");
        expect(state.incidents).toEqual([]);
    });

    it("passes severity and status filters to the API", async () => {
        const incidents = [
            {
                id: "incident-1",
                computeNodeId: "node-1",
                title: "GPU failure",
                description: "GPU failure detected.",
                severity: "High" as const,
                status: "Open" as const,
                createdAt: "2026-10-05T10:00:00Z",
            },
        ];

        mockedIncidentsApi.getAll.mockResolvedValue(incidents);

        const store = configureStore({
            reducer: {
                incidents: incidentsReducer,
            },
        });

        await store.dispatch(
            fetchIncidents({
                severity: "High",
                status: "Open",
            }),
        );

        expect(mockedIncidentsApi.getAll).toHaveBeenCalledWith({
            severity: "High",
            status: "Open",
        });

        expect(store.getState().incidents.incidents).toEqual(
            incidents,
        );
    });

    it("tracks loading state when fetching incidents", async () => {
        let resolveFetch: (
            value: {
                id: string;
                computeNodeId: string;
                title: string;
                description: string;
                severity: "Critical";
                status: "Open";
                createdAt: string;
            }[],
        ) => void;

        const fetchPromise = new Promise<
            {
                id: string;
                computeNodeId: string;
                title: string;
                description: string;
                severity: "Critical";
                status: "Open";
                createdAt: string;
            }[]
        >((resolve) => {
            resolveFetch = resolve;
        });

        mockedIncidentsApi.getAll.mockReturnValue(fetchPromise);

        const store = configureStore({
            reducer: {
                incidents: incidentsReducer,
            },
        });

        const promise = store.dispatch(fetchIncidents());

        expect(store.getState().incidents.loading).toBe(true);

        resolveFetch!([
            {
                id: "incident-1",
                computeNodeId: "node-1",
                title: "GPU failure",
                description: "GPU failure detected.",
                severity: "Critical",
                status: "Open",
                createdAt: "2026-10-05T10:00:00Z",
            },
        ]);

        await promise;

        expect(store.getState().incidents.loading).toBe(false);
    });

    it("clears the incident error", () => {
        const store = configureStore({
            reducer: {
                incidents: incidentsReducer,
            },
        });

        store.dispatch({
            type: "incidents/fetchIncidents/rejected",
            error: {
                message: "API unavailable",
            },
        });

        expect(store.getState().incidents.error).toBe(
            "API unavailable",
        );

        store.dispatch(clearIncidentError());

        expect(store.getState().incidents.error).toBeNull();
    });

    it("uses the fallback error message when fetchIncidents fails without a message", async () => {
        mockedIncidentsApi.getAll.mockRejectedValue(
            new Error(),
        );

        const store = configureStore({
            reducer: {
                incidents: incidentsReducer,
            },
        });

        await store.dispatch(fetchIncidents());

        const state = store.getState().incidents;

        expect(state.loading).toBe(false);
        expect(state.error).toBe(
            "Unable to load incidents. Please try again.",
        );
    });
});