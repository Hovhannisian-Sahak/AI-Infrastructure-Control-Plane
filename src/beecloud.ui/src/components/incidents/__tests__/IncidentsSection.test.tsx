import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Provider } from "react-redux";
import IncidentsSection from "../IncidentsSection";
import { incidentsApi } from "@/lib/api/incidentsApi";
import { createTestStore } from "@/test-utils";
import type { Incident } from "@/lib/api/models/incident";
import type { ComputeNode } from "@/lib/api/models/computeNode";

jest.mock("@/lib/api/incidentsApi");
const mockedIncidentsApi = jest.mocked(incidentsApi);

const incident: Incident = {
    id: "incident-1",
    computeNodeId: "node-1",
    severity: "Medium",
    status: "Open",
    title: "Compute node health check failed",
    description: "Health check failed on GPU node.",
    createdAt: "2026-10-05T10:00:00Z",
    updatedAt: "2026-10-05T10:00:00Z",
    resolvedAt: null,
};
const secondIncident: Incident = {
    ...incident,
    id: "incident-2",
    computeNodeId: "node-2",
    severity: "Critical",
    title: "GPU failure detected",
};

const page = (items: Incident[], totalCount = items.length, currentPage = 1) => ({
    items,
    page: currentPage,
    pageSize: 12,
    totalCount,
});

function renderSection(
    preloadedIncidents: Incident[] = [],
    nodes: ComputeNode[] = [],
) {
    const store = createTestStore({
        incidents: { incidents: preloadedIncidents },
        nodes: { nodes },
    });
    return {
        store,
        ...render(<Provider store={store}><IncidentsSection /></Provider>),
    };
}

beforeEach(() => {
    jest.resetAllMocks();
    mockedIncidentsApi.search.mockReturnValue(new Promise(() => {}));
});

describe("IncidentsSection", () => {
    it("requests the initial server page and displays its results", async () => {
        mockedIncidentsApi.search.mockResolvedValue(page([incident]));
        renderSection();

        expect(await screen.findByRole("heading", {
            name: incident.title,
        })).toBeInTheDocument();
        expect(mockedIncidentsApi.search).toHaveBeenCalledWith({
            page: 1,
            pageSize: 12,
        });
    });

    it("requests server filters and uses the returned fleet-wide total", async () => {
        mockedIncidentsApi.search.mockResolvedValue(page([secondIncident], 28));
        renderSection([], [
            {
                id: "node-1",
                name: "GPU Node 1",
                gpuModel: "NVIDIA A100",
                gpuCount: 4,
                status: "Available",
                activeFault: "None",
            },
            {
                id: "node-2",
                name: "GPU Node 2",
                gpuModel: "NVIDIA H100",
                gpuCount: 8,
                status: "Running",
                activeFault: "None",
            },
        ]);

        const user = userEvent.setup();
        await user.selectOptions(screen.getByLabelText("Severity"), "Critical");
        await user.selectOptions(screen.getByLabelText("Node"), "node-2");
        await waitFor(() => {
            expect(mockedIncidentsApi.search).toHaveBeenLastCalledWith({
                page: 1,
                pageSize: 12,
                severity: "Critical",
                computeNodeId: "node-2",
            });
        });
        expect(await screen.findByRole("heading", {
            name: secondIncident.title,
        })).toBeInTheDocument();
        expect(screen.getByText("28 incidents")).toBeInTheDocument();
    });

    it("sends date bounds to the server query", async () => {
        mockedIncidentsApi.search.mockResolvedValue(page([incident]));
        renderSection();

        fireEvent.change(screen.getByLabelText("From"), {
            target: { value: "2026-10-05" },
        });

        await waitFor(() => {
            expect(mockedIncidentsApi.search).toHaveBeenLastCalledWith({
                page: 1,
                pageSize: 12,
                from: "2026-10-05T00:00:00.000Z",
            });
        });
    });

    it("requests the next server page", async () => {
        mockedIncidentsApi.search
            .mockResolvedValueOnce(page([incident], 25))
            .mockResolvedValueOnce(page([secondIncident], 25, 2));
        renderSection();

        await screen.findByRole("heading", { name: incident.title });
        await userEvent.setup().click(screen.getByRole("button", { name: "Next" }));
        await waitFor(() => {
            expect(mockedIncidentsApi.search).toHaveBeenLastCalledWith({
                page: 2,
                pageSize: 12,
            });
        });
        expect(await screen.findByRole("heading", {
            name: secondIncident.title,
        })).toBeInTheDocument();
    });

    it("shows an empty state for no matching incidents", async () => {
        mockedIncidentsApi.search.mockResolvedValue(page([]));
        renderSection();

        expect(await screen.findByRole("heading", {
            name: "No incidents",
        })).toBeInTheDocument();
    });

    it("shows query errors", async () => {
        mockedIncidentsApi.search.mockRejectedValue(new Error("Failed to load incidents"));
        renderSection();

        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Failed to load incidents",
        );
    });
});
