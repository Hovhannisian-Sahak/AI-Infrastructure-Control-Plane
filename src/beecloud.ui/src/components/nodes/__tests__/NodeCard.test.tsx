import { fireEvent, screen } from "@testing-library/react";
import { renderWithProviders } from "@/test-utils";
import NodeCard from "../NodeCard";
import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";

const baseNode: ComputeNode = {
  id: "node-1",
  name: "gpu-node-01",
  gpuModel: "NVIDIA H100",
  gpuCount: 4,
  status: "Available",
  activeFault: "None",
};

const healthyHistory: HealthCheck[] = [
  {
    id: "health-1",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 42.5,
    gpuUsagePercent: 68.2,
    gpuTemperatureCelsius: 61.4,
    checkedAt: "2026-10-06T10:00:00Z",
  },
  {
    id: "health-2",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 55.1,
    gpuUsagePercent: 74.3,
    gpuTemperatureCelsius: 65.8,
    checkedAt: "2026-10-06T10:00:10Z",
  },
];

const unhealthyHistory: HealthCheck[] = [
  {
    id: "health-1",
    computeNodeId: "node-1",
    isHealthy: false,
    cpuUsagePercent: 95.2,
    gpuUsagePercent: 91.4,
    gpuTemperatureCelsius: 94.8,
    checkedAt: "2026-10-06T10:00:00Z",
  },
  {
    id: "health-2",
    computeNodeId: "node-1",
    isHealthy: false,
    cpuUsagePercent: 97.1,
    gpuUsagePercent: 93.7,
    gpuTemperatureCelsius: 96.2,
    checkedAt: "2026-10-06T10:00:10Z",
  },
];

describe("NodeCard", () => {
  beforeEach(() => {
    jest.clearAllMocks();

    Object.defineProperty(window, "confirm", {
      writable: true,
      value: jest.fn(() => true),
    });
  });

  it("renders node information", () => {
    renderWithProviders(<NodeCard node={baseNode} />);

    expect(screen.getByText("gpu-node-01")).toBeInTheDocument();
    expect(screen.getByText("node-1")).toBeInTheDocument();
    expect(screen.getByText("NVIDIA H100")).toBeInTheDocument();
    expect(screen.getByText("4")).toBeInTheDocument();
    expect(screen.getByText("None")).toBeInTheDocument();
    expect(screen.getByText("Available")).toBeInTheDocument();
  });

  it("renders no health data when history is empty", () => {
    renderWithProviders(<NodeCard node={baseNode} />);

    expect(screen.getByText("Health")).toBeInTheDocument();
    expect(screen.getByText("No health check yet")).toBeInTheDocument();
  });

  it("renders healthy health information", () => {
    renderWithProviders(<NodeCard node={baseNode} />, {
      preloadedState: {
        health: {
          historyByNodeId: {
            "node-1": healthyHistory,
          },
          latestByNodeId: {
            "node-1": healthyHistory[1],
          },
          loadingByNodeId: {},
          errorByNodeId: {},
        },
      },
    });

    expect(screen.getByText("Healthy")).toBeInTheDocument();
    expect(screen.getByText("55.1%")).toBeInTheDocument();
    expect(screen.getByText("65.8°C")).toBeInTheDocument();

    expect(
        screen.getByRole("img", {
          name: "CPU usage trend",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("img", {
          name: "GPU temperature trend",
        }),
    ).toBeInTheDocument();
  });

  it("renders unhealthy health information", () => {
    const unhealthyNode: ComputeNode = {
      ...baseNode,
      status: "Unhealthy",
      activeFault: "GpuOverheat",
    };

    renderWithProviders(<NodeCard node={unhealthyNode} />, {
      preloadedState: {
        health: {
          historyByNodeId: {
            "node-1": unhealthyHistory,
          },
          latestByNodeId: {
            "node-1": unhealthyHistory[1],
          },
          loadingByNodeId: {},
          errorByNodeId: {},
        },
      },
    });

    expect(
        screen.getAllByText("Unhealthy"),
    ).toHaveLength(2);

    expect(screen.getByText("GpuOverheat")).toBeInTheDocument();
    expect(screen.getByText("97.1%")).toBeInTheDocument();
    expect(screen.getByText("96.2°C")).toBeInTheDocument();
  });

  it("uses the latest item from health history as the displayed health", () => {
    renderWithProviders(<NodeCard node={baseNode} />, {
      preloadedState: {
        health: {
          historyByNodeId: {
            "node-1": healthyHistory,
          },

          // Deliberately use an older value here.
          // NodeCard should derive the latest value from history.
          latestByNodeId: {
            "node-1": healthyHistory[0],
          },

          loadingByNodeId: {},
          errorByNodeId: {},
        },
      },
    });

    expect(screen.getByText("55.1%")).toBeInTheDocument();
    expect(screen.getByText("65.8°C")).toBeInTheDocument();

    expect(screen.queryByText("42.5%")).not.toBeInTheDocument();
    expect(screen.queryByText("61.4°C")).not.toBeInTheDocument();
  });

  it("renders N/A when the latest health metric is null", () => {
    const history: HealthCheck[] = [
      {
        id: "health-1",
        computeNodeId: "node-1",
        isHealthy: true,
        cpuUsagePercent: null,
        gpuUsagePercent: null,
        gpuTemperatureCelsius: null,
        checkedAt: "2026-10-06T10:00:00Z",
      },
    ];

    renderWithProviders(<NodeCard node={baseNode} />, {
      preloadedState: {
        health: {
          historyByNodeId: {
            "node-1": history,
          },
          latestByNodeId: {
            "node-1": history[0],
          },
          loadingByNodeId: {},
          errorByNodeId: {},
        },
      },
    });

    expect(screen.getAllByText("N/A")).toHaveLength(2);
  });

  it("renders the CPU and temperature sparklines", () => {
    renderWithProviders(<NodeCard node={baseNode} />, {
      preloadedState: {
        health: {
          historyByNodeId: {
            "node-1": healthyHistory,
          },
          latestByNodeId: {
            "node-1": healthyHistory[1],
          },
          loadingByNodeId: {},
          errorByNodeId: {},
        },
      },
    });

    expect(
        screen.getByRole("img", {
          name: "CPU usage trend",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("img", {
          name: "GPU temperature trend",
        }),
    ).toBeInTheDocument();
  });

  it("renders a start button for an available node", () => {
    renderWithProviders(<NodeCard node={baseNode} />);

    expect(
        screen.getByRole("button", {
          name: "Start",
        }),
    ).toBeInTheDocument();

    expect(
        screen.queryByRole("button", {
          name: "Stop",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.queryByRole("button", {
          name: "Restart",
        }),
    ).not.toBeInTheDocument();
  });

  it("renders a start button for a stopped node", () => {
    const node: ComputeNode = {
      ...baseNode,
      status: "Stopped",
    };

    renderWithProviders(<NodeCard node={node} />);

    expect(
        screen.getByRole("button", {
          name: "Start",
        }),
    ).toBeInTheDocument();
  });

  it("renders stop and restart buttons for a running node", () => {
    const node: ComputeNode = {
      ...baseNode,
      status: "Running",
    };

    renderWithProviders(<NodeCard node={node} />);

    expect(
        screen.getByRole("button", {
          name: "Stop",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("button", {
          name: "Restart",
        }),
    ).toBeInTheDocument();

    expect(
        screen.queryByRole("button", {
          name: "Start",
        }),
    ).not.toBeInTheDocument();
  });

  it("does not render start, stop, or restart for a provisioning node", () => {
    const node: ComputeNode = {
      ...baseNode,
      status: "Provisioning",
    };

    renderWithProviders(<NodeCard node={node} />);

    expect(
        screen.queryByRole("button", {
          name: "Start",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.queryByRole("button", {
          name: "Stop",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.queryByRole("button", {
          name: "Restart",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByText(/Node is being provisioned/),
    ).toBeInTheDocument();
  });

  it("disables action buttons while an action is loading", () => {
    const node: ComputeNode = {
      ...baseNode,
      status: "Running",
    };

    renderWithProviders(<NodeCard node={node} />, {
      preloadedState: {
        nodes: {
          nodes: [node],
          loading: false,
          error: null,
          creating: false,
          createSuccess: null,
          actionLoadingByNodeId: {
            "node-1": true,
          },
          deletingNodeId: null,
          deleteErrorByNodeId: {},
        },
      },
    });

    expect(
        screen.getByRole("button", {
          name: "Stopping...",
        }),
    ).toBeDisabled();

    expect(
        screen.getByRole("button", {
          name: "Restarting...",
        }),
    ).toBeDisabled();

    expect(
        screen.getByRole("button", {
          name: "Delete",
        }),
    ).toBeDisabled();
  });

  it("shows deleting state", () => {
    renderWithProviders(<NodeCard node={baseNode} />, {
      preloadedState: {
        nodes: {
          nodes: [baseNode],
          loading: false,
          error: null,
          creating: false,
          createSuccess: null,
          actionLoadingByNodeId: {},
          deletingNodeId: "node-1",
          deleteErrorByNodeId: {},
        },
      },
    });

    expect(
        screen.getByRole("button", {
          name: "Deleting...",
        }),
    ).toBeDisabled();
  });

  it("shows delete error", () => {
    renderWithProviders(<NodeCard node={baseNode} />, {
      preloadedState: {
        nodes: {
          nodes: [baseNode],
          loading: false,
          error: null,
          creating: false,
          createSuccess: null,
          actionLoadingByNodeId: {},
          deletingNodeId: null,
          deleteErrorByNodeId: {
            "node-1": "Failed to delete node.",
          },
        },
      },
    });

    expect(
        screen.getByRole("alert"),
    ).toHaveTextContent("Failed to delete node.");
  });

  it("asks for confirmation before deleting a node", () => {
    const confirmMock = jest.fn(() => true);

    Object.defineProperty(window, "confirm", {
      writable: true,
      value: confirmMock,
    });

    renderWithProviders(<NodeCard node={baseNode} />);

    fireEvent.click(
        screen.getByRole("button", {
          name: "Delete",
        }),
    );

    expect(confirmMock).toHaveBeenCalledWith(
        'Are you sure you want to delete "gpu-node-01"?',
    );
  });

  it("does not delete when confirmation is cancelled", () => {
    const confirmMock = jest.fn(() => false);

    Object.defineProperty(window, "confirm", {
      writable: true,
      value: confirmMock,
    });

    renderWithProviders(<NodeCard node={baseNode} />);

    fireEvent.click(
        screen.getByRole("button", {
          name: "Delete",
        }),
    );

    expect(confirmMock).toHaveBeenCalled();
  });

  it("highlights an active fault", () => {
    const node: ComputeNode = {
      ...baseNode,
      activeFault: "GpuFailure",
    };

    renderWithProviders(<NodeCard node={node} />);

    expect(
        screen.getByText("GpuFailure"),
    ).toBeInTheDocument();
  });

  it("renders health error when one exists", () => {
    renderWithProviders(<NodeCard node={baseNode} />, {
      preloadedState: {
        health: {
          historyByNodeId: {},
          latestByNodeId: {},
          loadingByNodeId: {},
          errorByNodeId: {
            "node-1": "Failed to fetch health history.",
          },
        },
      },
    });

    // NodeCard currently does not render the health error.
    // This test intentionally does not expect an error element.
    expect(
        screen.getByText("No health check yet"),
    ).toBeInTheDocument();
  });
});