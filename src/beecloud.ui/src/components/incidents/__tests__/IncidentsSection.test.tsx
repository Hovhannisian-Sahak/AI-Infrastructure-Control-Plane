import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
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
    lastSeenAt: "2026-10-05T10:00:00Z",
    occurrenceCount: 1,
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
            name: "Active incidents",
        })).toBeInTheDocument();
        expect(await screen.findByRole("heading", {
            name: incident.title,
        })).toBeInTheDocument();
        expect(mockedIncidentsApi.search).toHaveBeenCalledWith({
            page: 1,
            pageSize: 12,
        });
    });

    it("automatically refreshes incidents and displays newly reported incidents", async () => {
        jest.useFakeTimers();
        mockedIncidentsApi.search
            .mockResolvedValueOnce(page([]))
            .mockResolvedValue(page([incident]));
        const rendered = renderSection();

        try {
            await act(async () => {
                await Promise.resolve();
            });
            expect(mockedIncidentsApi.search).toHaveBeenCalledTimes(1);

            await act(async () => {
                await jest.advanceTimersByTimeAsync(5_000);
            });

            expect(mockedIncidentsApi.search).toHaveBeenCalledTimes(2);
            expect(screen.getByRole("heading", {
                name: incident.title,
            })).toBeInTheDocument();
        } finally {
            rendered.unmount();
            jest.useRealTimers();
        }
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

    it("groups resolved incidents under history after active incidents", async () => {
        const resolvedIncident: Incident = {
            ...incident,
            id: "incident-resolved",
            status: "Resolved",
            title: "Resolved GPU incident",
            resolvedAt: "2026-10-05T11:00:00Z",
        };
        mockedIncidentsApi.search.mockResolvedValue(
            page([secondIncident, resolvedIncident, incident]),
        );
        renderSection();

        const activeHeading = await screen.findByRole("heading", {
            name: "Active incidents",
        });
        const historyHeading = screen.getByRole("heading", {
            name: "Incident history",
        });

        expect(activeHeading.compareDocumentPosition(historyHeading))
            .toBe(Node.DOCUMENT_POSITION_FOLLOWING);
        expect(screen.getByRole("heading", { name: secondIncident.title }))
            .toBeInTheDocument();
        expect(screen.getByRole("heading", { name: incident.title }))
            .toBeInTheDocument();
        const historyGroup = document.querySelector("details");
        expect(historyGroup).not.toHaveAttribute("open");
        await userEvent.setup().click(historyGroup!.querySelector("summary")!);
        expect(historyGroup).toHaveAttribute("open");
        expect(screen.getByRole("heading", { name: resolvedIncident.title }))
            .toBeInTheDocument();
    });

    it("groups all resolved episodes under their node in history", async () => {
        const olderResolvedIncident: Incident = {
            ...incident,
            id: "incident-resolved-older",
            status: "Resolved",
            title: "Older resolved incident",
            resolvedAt: "2026-10-05T11:00:00Z",
        };
        const latestResolvedIncident: Incident = {
            ...olderResolvedIncident,
            id: "incident-resolved-latest",
            title: "Latest resolved incident",
            resolvedAt: "2026-10-05T12:00:00Z",
        };
        mockedIncidentsApi.search.mockResolvedValue(
            page([olderResolvedIncident, latestResolvedIncident]),
        );
        renderSection();

        expect(await screen.findByText("Older resolved incident"))
            .toBeInTheDocument();
        expect(screen.getByRole("heading", { name: "Incident history" }))
            .toBeInTheDocument();
        const historyGroup = document.querySelector("details");
        expect(historyGroup).not.toHaveAttribute("open");
        await userEvent.setup().click(historyGroup!.querySelector("summary")!);
        expect(historyGroup).toHaveAttribute("open");
        expect(screen.getByRole("heading", {
            name: "Latest resolved incident",
        })).toBeInTheDocument();
        expect(screen.getByRole("heading", {
            name: "Older resolved incident",
        })).toBeInTheDocument();
        expect(screen.getAllByRole("article")).toHaveLength(2);
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

    it("keeps the incident list and pagination mounted while a page loads", async () => {
        let resolvePage!: (value: ReturnType<typeof page>) => void;
        mockedIncidentsApi.search
            .mockResolvedValueOnce(page([incident], 25))
            .mockReturnValueOnce(new Promise(resolve => {
                resolvePage = resolve;
            }));
        renderSection();

        await screen.findByRole("heading", { name: incident.title });
        await userEvent.setup().click(screen.getByRole("button", { name: "Next" }));

        expect(screen.getByRole("heading", { name: incident.title }))
            .toBeInTheDocument();
        expect(screen.getByRole("navigation", { name: "Incident pages" }))
            .toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
        expect(document.querySelector('[aria-busy="true"]')).toBeInTheDocument();

        await act(async () => {
            resolvePage(page([secondIncident], 25, 2));
        });

        expect(await screen.findByRole("heading", { name: secondIncident.title }))
            .toBeInTheDocument();
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
