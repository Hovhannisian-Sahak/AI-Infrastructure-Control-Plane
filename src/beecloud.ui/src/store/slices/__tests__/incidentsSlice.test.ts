import { configureStore } from "@reduxjs/toolkit";
import incidentsReducer, {
    fetchIncidents,
    clearIncidentError,
} from "../incidentsSlice";
import { incidentsApi } from "@/lib/api/incidentsApi";

jest.mock("@/lib/api/incidentsApi");

const mockedIncidentsApi = jest.mocked(incidentsApi);
const incident = {
    id: "incident-1",
    computeNodeId: "node-1",
    title: "GPU failure",
    description: "GPU failure detected.",
    severity: "Critical" as const,
    status: "Open" as const,
    createdAt: "2026-10-05T10:00:00Z",
    updatedAt: "2026-10-05T10:00:00Z",
    lastSeenAt: "2026-10-05T10:00:00Z",
    occurrenceCount: 1,
};
const result = {
    items: [incident],
    page: 2,
    pageSize: 12,
    totalCount: 25,
};

function createStore() {
    return configureStore({
        reducer: { incidents: incidentsReducer },
    });
}

describe("incidentsSlice", () => {
    beforeEach(() => jest.resetAllMocks());

    it("stores a server-paged incident response", async () => {
        mockedIncidentsApi.search.mockResolvedValue(result);
        const store = createStore();

        await store.dispatch(fetchIncidents({
            page: 2,
            pageSize: 12,
            severity: "Critical",
        }));

        expect(store.getState().incidents).toMatchObject({
            incidents: result.items,
            page: 2,
            pageSize: 12,
            totalCount: 25,
            loading: false,
            error: null,
        });
        expect(mockedIncidentsApi.search).toHaveBeenCalledWith({
            page: 2,
            pageSize: 12,
            severity: "Critical",
        });
    });

    it("tracks loading state while the request is pending", async () => {
        let resolveRequest!: (value: typeof result) => void;
        mockedIncidentsApi.search.mockReturnValue(new Promise(resolve => {
            resolveRequest = resolve;
        }));
        const store = createStore();

        const request = store.dispatch(fetchIncidents({
            page: 1,
            pageSize: 12,
        }));
        expect(store.getState().incidents.loading).toBe(true);

        resolveRequest(result);
        await request;
        expect(store.getState().incidents.loading).toBe(false);
    });

    it("reports server errors and clears them on demand", async () => {
        mockedIncidentsApi.search.mockRejectedValue(new Error("API unavailable"));
        const store = createStore();

        await store.dispatch(fetchIncidents({ page: 1, pageSize: 12 }));
        expect(store.getState().incidents.error).toBe("API unavailable");

        store.dispatch(clearIncidentError());
        expect(store.getState().incidents.error).toBeNull();
    });

    it("ignores an older response after a newer query has started", async () => {
        let resolveFirst!: (value: typeof result) => void;
        mockedIncidentsApi.search
            .mockReturnValueOnce(new Promise(resolve => { resolveFirst = resolve; }))
            .mockResolvedValueOnce({ ...result, items: [], page: 1, totalCount: 0 });
        const store = createStore();

        const first = store.dispatch(fetchIncidents({ page: 1, pageSize: 12 }));
        await store.dispatch(fetchIncidents({ page: 1, pageSize: 12, status: "Resolved" }));
        resolveFirst(result);
        await first;

        expect(store.getState().incidents.incidents).toEqual([]);
        expect(store.getState().incidents.totalCount).toBe(0);
    });
});
