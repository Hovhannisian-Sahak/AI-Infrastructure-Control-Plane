import {
    render,
    screen,
    waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { within } from "@testing-library/react";
import { Provider } from "react-redux";

import NodeDetailPage from "../NodeDetailPage";

import { createTestStore } from "@/test-utils";
import { nodesApi } from "@/lib/api/nodesApi";

jest.mock("@/lib/api/nodesApi");

jest.mock("next/navigation", () => ({
    useParams: () => ({
        nodeId: "node-1",
    }),
}));

const mockedNodesApi =
    jest.mocked(nodesApi);

const node = {
    id: "node-1",
    name: "gpu-node-01",
    gpuModel: "NVIDIA H100",
    gpuCount: 4,
    status: "Running" as const,
    activeFault: "None" as const,
};

const health1 = {
    id: "health-1",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 40,
    gpuUsagePercent: 60,
    gpuTemperatureCelsius: 65,
    checkedAt: "2026-10-07T10:00:00Z",
};

const health2 = {
    id: "health-2",
    computeNodeId: "node-1",
    isHealthy: false,
    cpuUsagePercent: 95,
    gpuUsagePercent: 80,
    gpuTemperatureCelsius: 96,
    checkedAt: "2026-10-07T11:00:00Z",
};

const metrics = [
    {
        id: "metric-1",
        computeNodeId: "node-1",
        cpuUsagePercent: 35,
        gpuUsagePercent: 70,
        gpuTemperatureCelsius: 66,
        recordedAt: "2026-10-07T10:00:00Z",
    },
];

function renderPage() {
    const store = createTestStore({
        nodes: {
            nodes: [node],
        },
        health: {
            historyByNodeId: {
                "node-1": [
                    health1,
                    health2,
                ],
            },
            latestByNodeId: {
                "node-1": health2,
            },
            loadingByNodeId: {
                "node-1": false,
            },
            errorByNodeId: {
                "node-1": null,
            },
        },
        metrics: {
            historyByNodeId: {
                "node-1": metrics,
            },
        },
    });

    return {
        store,
        ...render(
            <Provider store={store}>
                <NodeDetailPage />
            </Provider>,
        ),
    };
}

beforeEach(() => {
    jest.resetAllMocks();

    mockedNodesApi.getHealthHistory.mockResolvedValue([
        health1,
        health2,
    ]);
    mockedNodesApi.getNodeMetrics.mockResolvedValue(metrics);
    mockedNodesApi.getHealthHistoryPage.mockResolvedValue({
        items: [health2, health1],
        nextCursor: null,
        previousCursor: null,
    });
    mockedNodesApi.getNodeMetricsPage.mockResolvedValue({
        items: metrics,
        nextCursor: null,
        previousCursor: null,
    });
});

describe("NodeDetailPage", () => {
    it("renders node information", () => {
        renderPage();

        expect(
            screen.getByRole("heading", {
                name: "gpu-node-01",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByText("NVIDIA H100"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("4", {
                selector: "strong",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByText("Running"),
        ).toBeInTheDocument();
    });

    it("renders current health", () => {
        renderPage();

        expect(
            screen.getAllByText("Unhealthy")
                .length,
        ).toBeGreaterThan(0);
    });

    it("renders health metrics", () => {
        renderPage();

        expect(
            screen.getAllByText("CPU Usage").length,
        ).toBeGreaterThan(0);

        expect(
            screen.getAllByText("GPU Usage").length,
        ).toBeGreaterThan(0);

        expect(
            screen.getAllByText("GPU Temperature").length,
        ).toBeGreaterThan(0);
    });

    it("renders health history", () => {
        renderPage();

        expect(
            screen.getByRole("heading", {
                name: "Health History",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByText(
                "2 displayed health checks",
            ),
        ).toBeInTheDocument();
    });

    it("renders metric history separately from health status history", () => {
        renderPage();

        expect(
            screen.getByRole("heading", { name: "Node Metrics" }),
        ).toBeInTheDocument();
        expect(screen.getAllByText("35.0%").length).toBeGreaterThan(0);
        expect(
            screen.getByRole("heading", { name: "Health History" }),
        ).toBeInTheDocument();
    });

    it("renders time range buttons", () => {
        renderPage();

        expect(
            screen.getByRole("button", {
                name: "1 hour",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByRole("button", {
                name: "6 hours",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByRole("button", {
                name: "24 hours",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByRole("button", {
                name: "7 days",
            }),
        ).toBeInTheDocument();
    });

    it("requests the selected time range from the backend", async () => {
        const user = userEvent.setup();

        renderPage();

        await waitFor(() => {
            expect(
                mockedNodesApi.getHealthHistory,
            ).toHaveBeenCalledTimes(1);
        });

        const initialRequest =
            mockedNodesApi.getHealthHistory.mock.calls[0];
        expect(initialRequest[0]).toBe("node-1");
        expect(initialRequest[1]).toBe(100);
        expect(Date.parse(initialRequest[3]!) - Date.parse(initialRequest[2]!))
            .toBe(24 * 60 * 60 * 1000);

        const button =
            screen.getByRole("button", {
                name: "1 hour",
            });

        await user.click(button);

        await waitFor(() => {
            expect(
                mockedNodesApi.getHealthHistory,
            ).toHaveBeenCalledTimes(2);
        });

        expect(button).toHaveAttribute("aria-pressed", "true");
        expect(
            screen.getByRole("button", {
                name: "24 hours",
            }),
        ).toHaveAttribute("aria-pressed", "false");

        const selectedRequest =
            mockedNodesApi.getHealthHistory.mock.calls[1];
        expect(selectedRequest[0]).toBe("node-1");
        expect(selectedRequest[1]).toBe(100);
        expect(Date.parse(selectedRequest[3]!) - Date.parse(selectedRequest[2]!))
            .toBe(60 * 60 * 1000);
    });

    it("fetches health history for the node", async () => {
        renderPage();

        await waitFor(() => {
            expect(
                mockedNodesApi.getHealthHistory,
            ).toHaveBeenCalledWith(
                "node-1",
                100,
                expect.any(String),
                expect.any(String),
            );
        });
    });

    it("requests node metrics with the selected backend range", async () => {
        renderPage();

        await waitFor(() => {
            expect(mockedNodesApi.getNodeMetrics).toHaveBeenCalledWith(
                "node-1",
                100,
                expect.any(String),
                expect.any(String),
            );
        });
    });

    it("requests the next health table cursor page", async () => {
        const user = userEvent.setup();
        mockedNodesApi.getHealthHistoryPage.mockResolvedValue({
            items: [health2],
            nextCursor: "health-next",
            previousCursor: null,
        });
        renderPage();

        const navigation = await screen.findByRole("navigation", {
            name: "Health history pages",
        });
        await user.click(within(navigation).getByRole("button", { name: "Older" }));

        await waitFor(() => {
            expect(mockedNodesApi.getHealthHistoryPage).toHaveBeenLastCalledWith(
                "node-1",
                expect.objectContaining({
                    cursor: "health-next",
                    previous: false,
                    limit: 25,
                }),
            );
        });
    });
});