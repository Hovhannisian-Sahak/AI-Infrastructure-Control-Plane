import {
    render,
    screen,
    within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import HealthDashboard from "../HealthDashboard";

const nodes = [
    {
        id: "node-1",
        name: "gpu-node-01",
        gpuModel: "NVIDIA H100",
        gpuCount: 4,
        status: "Running" as const,
        activeFault: "None" as const,
    },
    {
        id: "node-2",
        name: "gpu-node-02",
        gpuModel: "NVIDIA A100",
        gpuCount: 2,
        status: "Unhealthy" as const,
        activeFault: "GpuFailure" as const,
    },
    {
        id: "node-3",
        name: "gpu-node-03",
        gpuModel: "NVIDIA L40S",
        gpuCount: 2,
        status: "Stopped" as const,
        activeFault: "None" as const,
    },
];

const healthy = {
    id: "health-1",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 50,
    gpuUsagePercent: 60,
    gpuTemperatureCelsius: 65,
    checkedAt: "2026-10-07T12:00:00Z",
};

const unhealthy = {
    id: "health-2",
    computeNodeId: "node-2",
    isHealthy: false,
    cpuUsagePercent: 95,
    gpuUsagePercent: 80,
    gpuTemperatureCelsius: 96,
    checkedAt: "2026-10-07T12:00:00Z",
};

function renderDashboard() {
    return render(
        <HealthDashboard
            nodes={nodes}
            historyByNodeId={{
                "node-1": [healthy],
                "node-2": [unhealthy],
            }}
            latestByNodeId={{
                "node-1": healthy,
                "node-2": unhealthy,
            }}
            loadingByNodeId={{
                "node-1": false,
                "node-2": false,
                "node-3": false,
            }}
        />,
    );
}

describe("HealthDashboard", () => {
    it("renders fleet summary", () => {
        renderDashboard();

        expect(
            screen.getByText("Total Nodes"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("Healthy", {
                selector: "span.summaryLabel",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByText("Unhealthy", {
                selector: "span.summaryLabel",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByText("No Data", {
                selector: "span.summaryLabel",
            }),
        ).toBeInTheDocument();
    });

    it("shows all nodes by default", () => {
        renderDashboard();

        expect(
            screen.getByText("gpu-node-01"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("gpu-node-02"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("gpu-node-03"),
        ).toBeInTheDocument();
    });

    it("filters healthy nodes", async () => {
        const user = userEvent.setup();

        renderDashboard();

        await user.click(
            screen.getByRole("button", {
                name: "Healthy",
            }),
        );

        expect(
            screen.getByText("gpu-node-01"),
        ).toBeInTheDocument();

        expect(
            screen.queryByText("gpu-node-02"),
        ).not.toBeInTheDocument();

        expect(
            screen.queryByText("gpu-node-03"),
        ).not.toBeInTheDocument();
    });

    it("filters unhealthy nodes", async () => {
        const user = userEvent.setup();

        renderDashboard();

        await user.click(
            screen.getByRole("button", {
                name: "Unhealthy",
            }),
        );

        expect(
            screen.getByText("gpu-node-02"),
        ).toBeInTheDocument();

        expect(
            screen.queryByText("gpu-node-01"),
        ).not.toBeInTheDocument();
    });

    it("filters nodes without health data", async () => {
        const user = userEvent.setup();

        renderDashboard();

        await user.click(
            screen.getByRole("button", {
                name: "No Data",
            }),
        );

        expect(
            screen.getByText("gpu-node-03"),
        ).toBeInTheDocument();

        expect(
            screen.queryByText("gpu-node-01"),
        ).not.toBeInTheDocument();

        expect(
            screen.queryByText("gpu-node-02"),
        ).not.toBeInTheDocument();
    });

    it("renders time range controls", () => {
        renderDashboard();

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

    it("shows node health metrics", () => {
        renderDashboard();

        expect(
            screen.getByText("50.0%"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("65.0°C"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("95.0%"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("96.0°C"),
        ).toBeInTheDocument();
    });

    it("links each node to its detail page", () => {
        renderDashboard();

        const nodeRow = screen.getByRole("row", {
            name: /gpu-node-01/,
        });

        expect(
            within(nodeRow).getByRole("link", {
                name: "Details",
            }),
        ).toHaveAttribute(
            "href",
            "/nodes/node-1",
        );
    });

    it("shows no-data status for nodes without health", () => {
        renderDashboard();

        const nodeRow = screen.getByRole("row", {
            name: /gpu-node-03.*No Data/,
        });

        expect(nodeRow).toBeInTheDocument();
    });
});