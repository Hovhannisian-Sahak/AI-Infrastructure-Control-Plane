import {
    render,
    screen,
    within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import type { HealthCheck } from "@/lib/api/models/healthCheck";
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

function renderDashboard(
    latestByNodeId: Record<
        string,
        HealthCheck | null | undefined
    > = {
        "node-1": healthy,
        "node-2": unhealthy,
    },
    historyByNodeId: Record<string, HealthCheck[]> = {
        "node-1": [healthy],
        "node-2": [unhealthy],
    },
) {
    return render(
        <HealthDashboard
            nodes={nodes}
            historyByNodeId={historyByNodeId}
            latestByNodeId={latestByNodeId}
            loadingByNodeId={{
                "node-1": false,
                "node-2": false,
                "node-3": false,
            }}
        />,
    );
}

function getNodeRow(nodeName: string) {
    return screen.getByRole("row", {
        name: new RegExp(nodeName),
    });
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

        expect(getNodeRow("gpu-node-01")).toBeInTheDocument();
        expect(getNodeRow("gpu-node-02")).toBeInTheDocument();
        expect(getNodeRow("gpu-node-03")).toBeInTheDocument();
    });

    it("filters healthy nodes", async () => {
        const user = userEvent.setup();

        renderDashboard();

        await user.click(
            screen.getByRole("button", {
                name: "Healthy",
            }),
        );

        expect(getNodeRow("gpu-node-01")).toBeInTheDocument();
        expect(
            screen.queryByRole("row", { name: /gpu-node-02/ }),
        ).not.toBeInTheDocument();
        expect(
            screen.queryByRole("row", { name: /gpu-node-03/ }),
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

        expect(getNodeRow("gpu-node-02")).toBeInTheDocument();
        expect(
            screen.queryByRole("row", { name: /gpu-node-01/ }),
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

        expect(getNodeRow("gpu-node-03")).toBeInTheDocument();
        expect(
            screen.queryByRole("row", { name: /gpu-node-01/ }),
        ).not.toBeInTheDocument();
        expect(
            screen.queryByRole("row", { name: /gpu-node-02/ }),
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

    it("notifies the page when the selected time range changes", async () => {
        const user = userEvent.setup();
        const onRangeChange = jest.fn();

        render(
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
                loadingByNodeId={{}}
                onRangeChange={onRangeChange}
            />,
        );

        await user.click(
            screen.getByRole("button", { name: "1 hour" }),
        );

        expect(onRangeChange).toHaveBeenCalledWith("1h");
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

    it("compares metrics side by side for multiple selected nodes", async () => {
        const user = userEvent.setup();
        renderDashboard();

        await user.click(
            screen.getByRole("checkbox", {
                name: "Compare gpu-node-01",
            }),
        );
        await user.click(
            screen.getByRole("checkbox", {
                name: "Compare gpu-node-02",
            }),
        );

        const comparison = screen.getByRole("table", {
            name: "Node comparison",
        });

        expect(comparison).toHaveTextContent("gpu-node-01");
        expect(comparison).toHaveTextContent("gpu-node-02");
        expect(comparison).toHaveTextContent("50.0%");
        expect(comparison).toHaveTextContent("95.0%");
        expect(comparison).toHaveTextContent("60.0%");
        expect(comparison).toHaveTextContent("80.0%");
        expect(comparison).toHaveTextContent("65.0°C");
        expect(comparison).toHaveTextContent("96.0°C");
        expect(comparison).toHaveTextContent("Healthy");
        expect(comparison).toHaveTextContent("Unhealthy");
        expect(
            within(comparison).getByRole("link", {
                name: "gpu-node-01",
            }),
        ).toHaveAttribute("href", "/nodes/node-1");
    });

    it("updates the comparison when a node is deselected", async () => {
        const user = userEvent.setup();
        renderDashboard();

        const firstNodeCheckbox = screen.getByRole("checkbox", {
            name: "Compare gpu-node-01",
        });

        await user.click(firstNodeCheckbox);
        expect(
            screen.getByRole("table", { name: "Node comparison" }),
        ).toHaveTextContent("gpu-node-01");

        await user.click(firstNodeCheckbox);
        expect(
            screen.queryByRole("table", { name: "Node comparison" }),
        ).not.toBeInTheDocument();
        expect(
            screen.getByText(
                "Select nodes in the health table to compare their latest readings.",
            ),
        ).toBeInTheDocument();
    });

    it("shows active unhealthy and high-usage alerts derived from latest readings", () => {
        renderDashboard();

        const alerts = screen.getByRole("region", {
            name: "Health Alerts",
        });

        expect(alerts).toHaveTextContent(
            "Node health check reports unhealthy.",
        );
        expect(alerts).toHaveTextContent(
            "CPU usage is high (95.0%).",
        );
        expect(alerts).toHaveTextContent(
            "GPU temperature is high (96.0°C).",
        );
        expect(alerts).not.toHaveTextContent("GPU usage is high");
        expect(
            within(alerts).getAllByRole("link", {
                name: "gpu-node-02",
            }),
        ).toHaveLength(3);
        within(alerts)
            .getAllByRole("link", {
                name: "gpu-node-02",
            })
            .forEach(link => {
                expect(link).toHaveAttribute(
                    "href",
                    "/nodes/node-2",
                );
            });
        expect(alerts).toHaveTextContent("Node currently affected");
    });

    it("shows resolved threshold crossings as historical alerts", () => {
        const past = {
            ...healthy,
            id: "health-past",
            gpuTemperatureCelsius: 94,
            checkedAt: new Date(
                Date.now() - 24 * 60 * 60 * 1000,
            ).toISOString(),
        };
        const latest = {
            ...healthy,
            id: "health-current",
        };

        renderDashboard(
            {
                "node-1": latest,
            },
            {
                "node-1": [past, latest],
            },
        );

        const alerts = screen.getByRole("region", {
            name: "Health Alerts",
        });
        const history = within(alerts).getByRole("region", {
            name: "History",
        });

        expect(history).toHaveTextContent(
            "GPU temperature reached 94.0°C.",
        );
        expect(history).not.toHaveTextContent("Node currently affected");
        expect(
            within(history).getByRole("link", {
                name: "gpu-node-01",
            }),
        ).toHaveAttribute("href", "/nodes/node-1");
    });

    it("flags high GPU usage even when the node is otherwise healthy", () => {
        renderDashboard({
            "node-1": {
                ...healthy,
                gpuUsagePercent: 92,
            },
        });

        const alerts = screen.getByRole("region", {
            name: "Health Alerts",
        });

        expect(alerts).toHaveTextContent(
            "GPU usage is high (92.0%).",
        );
        expect(
            within(alerts).getByRole("link", {
                name: "gpu-node-01",
            }),
        ).toHaveAttribute("href", "/nodes/node-1");
        expect(alerts).not.toHaveTextContent(
            "Node health check reports unhealthy.",
        );
    });

    it("shows an empty state when there are no active alerts", () => {
        renderDashboard({
            "node-1": healthy,
            "node-2": {
                ...unhealthy,
                isHealthy: true,
                cpuUsagePercent: 50,
                gpuUsagePercent: 60,
                gpuTemperatureCelsius: 65,
            },
        });

        expect(
            within(
                screen.getByRole("region", { name: "Active" }),
            ).getByRole("status"),
        ).toHaveTextContent("No active health alerts.");
    });

    it("keeps historical health visible for unmonitored nodes without raising current alerts", () => {
        const stoppedNode = {
            ...nodes[2],
            status: "Stopped" as const,
        };
        const oldHealth = {
            ...healthy,
            computeNodeId: stoppedNode.id,
            cpuUsagePercent: 42,
            gpuUsagePercent: 95,
        };

        render(
            <HealthDashboard
                nodes={[stoppedNode]}
                historyByNodeId={{
                    [stoppedNode.id]: [oldHealth],
                }}
                latestByNodeId={{
                    [stoppedNode.id]: oldHealth,
                }}
                loadingByNodeId={{ [stoppedNode.id]: false }}
            />,
        );

        expect(getNodeRow("gpu-node-03")).toHaveTextContent("42.0%");
        expect(getNodeRow("gpu-node-03")).toHaveTextContent("95.0%");
        expect(
            screen.getByRole("status"),
        ).toHaveTextContent("No active health alerts.");
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