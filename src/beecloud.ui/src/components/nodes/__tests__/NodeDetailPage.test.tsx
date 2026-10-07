import {
    render,
    screen,
    waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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
            screen.getByText("CPU Usage"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("GPU Usage"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("GPU Temperature"),
        ).toBeInTheDocument();
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
                "2 recent health checks",
            ),
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
});