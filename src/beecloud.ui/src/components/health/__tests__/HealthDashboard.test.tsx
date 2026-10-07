import {
    render,
    screen,
    within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import type { HealthCheck } from "@/lib/api/models/healthCheck";
import type { NodeMetric } from "@/lib/api/models/nodeMetric";
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

const metricsByNodeId: Record<string, NodeMetric[]> = {
    "node-1": [{
        id: "metric-1",
        computeNodeId: "node-1",
        cpuUsagePercent: 50,
        gpuUsagePercent: 60,
        gpuTemperatureCelsius: 65,
        recordedAt: "2026-10-07T12:00:00Z",
    }],
    "node-2": [{
        id: "metric-2",
        computeNodeId: "node-2",
        cpuUsagePercent: 95,
        gpuUsagePercent: 80,
        gpuTemperatureCelsius: 96,
        recordedAt: "2026-10-07T12:00:00Z",
    }],
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
            metricsByNodeId={metricsByNodeId}
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
                metricsByNodeId={metricsByNodeId}
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

    it("uses node metric telemetry for resource values and health checks for status", () => {
        const metric: NodeMetric = {
            id: "independent-metric",
            computeNodeId: "node-1",
            cpuUsagePercent: 82,
            gpuUsagePercent: 77,
            gpuTemperatureCelsius: 71,
            recordedAt: "2026-10-07T12:01:00Z",
        };

        render(
            <HealthDashboard
                nodes={[nodes[0]]}
                historyByNodeId={{
                    "node-1": [{
                        ...healthy,
                        cpuUsagePercent: 12,
                    }],
                }}
                latestByNodeId={{
                    "node-1": {
                        ...healthy,
                        cpuUsagePercent: 12,
                    },
                }}
                metricsByNodeId={{ "node-1": [metric] }}
            />,
        );

        const row = getNodeRow("gpu-node-01");
        expect(row).toHaveTextContent("82.0%");
        expect(row).not.toHaveTextContent("12.0%");
        expect(row).toHaveTextContent("Healthy");
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

    it("paginates historical alerts without changing the total count", async () => {
        const user = userEvent.setup();
        const fleet = Array.from({ length: 11 }, (_, index) => {
            const node = {
                ...nodes[0],
                id: `history-node-${index}`,
                name: `history-node-${String(index + 1).padStart(2, "0")}`,
            };
            const current = {
                ...healthy,
                id: `current-${index}`,
                computeNodeId: node.id,
                checkedAt: new Date().toISOString(),
            };
            const old = {
                ...unhealthy,
                id: `old-${index}`,
                computeNodeId: node.id,
                cpuUsagePercent: null,
                gpuUsagePercent: null,
                gpuTemperatureCelsius: null,
                checkedAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
            };
            return { node, current, old };
        });

        render(
            <HealthDashboard
                nodes={fleet.map(item => item.node)}
                historyByNodeId={Object.fromEntries(
                    fleet.map(item => [item.node.id, [item.old, item.current]]),
                )}
                latestByNodeId={Object.fromEntries(
                    fleet.map(item => [item.node.id, item.current]),
                )}
            />,
        );

        const alerts = screen.getByRole("region", { name: "Health Alerts" });
        const history = within(alerts).getByRole("region", { name: "History" });
        expect(within(alerts).getByLabelText("11 alerts")).toBeInTheDocument();
        const firstPageLinks = within(history).getAllByRole("link");
        expect(firstPageLinks).toHaveLength(10);
        const firstPageNames = firstPageLinks.map(link => link.textContent);

        await user.click(within(history).getByRole("button", { name: "Next" }));

        const secondPageLinks = within(history).getAllByRole("link");
        expect(secondPageLinks).toHaveLength(1);
        expect(firstPageNames).not.toContain(secondPageLinks[0].textContent);
        expect(within(alerts).getByLabelText("11 alerts")).toBeInTheDocument();
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
                metricsByNodeId={{
                    [stoppedNode.id]: [{
                        id: "metric-stopped",
                        computeNodeId: stoppedNode.id,
                        cpuUsagePercent: 42,
                        gpuUsagePercent: 95,
                        gpuTemperatureCelsius: 75,
                        recordedAt: oldHealth.checkedAt,
                    }],
                }}
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

    it("paginates the displayed rows while keeping fleet summaries and comparisons global", async () => {
        const user = userEvent.setup();
        const fleet = Array.from({ length: 12 }, (_, index) => ({
            ...nodes[0],
            id: `node-${index + 1}`,
            name: `gpu-node-${String(index + 1).padStart(2, "0")}`,
        }));
        const latestById = Object.fromEntries(
            fleet.map((node, index) => [
                node.id,
                index === 11 ? unhealthy : healthy,
            ]),
        );

        render(
            <HealthDashboard
                nodes={fleet}
                historyByNodeId={{}}
                latestByNodeId={latestById}
            />,
        );

        expect(screen.getByText("12", { selector: "strong" })).toBeInTheDocument();
        expect(getNodeRow("gpu-node-10")).toBeInTheDocument();
        expect(screen.queryByRole("row", { name: /gpu-node-11/ })).not.toBeInTheDocument();

        await user.click(screen.getByRole("checkbox", {
            name: "Compare gpu-node-01",
        }));
        await user.click(screen.getByRole("button", { name: "Next" }));

        expect(getNodeRow("gpu-node-11")).toBeInTheDocument();
        expect(getNodeRow("gpu-node-12")).toBeInTheDocument();
        expect(screen.getByRole("table", { name: "Node comparison" }))
            .toHaveTextContent("gpu-node-01");
        expect(screen.getByRole("region", { name: "Health Alerts" }))
            .toHaveTextContent("gpu-node-12");
    });
});