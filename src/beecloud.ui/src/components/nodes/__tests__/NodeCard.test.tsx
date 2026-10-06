import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { renderWithProviders } from "@/test-utils";
import NodeCard from "@/components/nodes/NodeCard";
import { nodesApi } from "@/lib/api/nodesApi";
import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";

jest.mock("@/lib/api/nodesApi");

const mockedNodesApi = jest.mocked(nodesApi);

const node: ComputeNode = {
  id: "node-1",
  name: "GPU Node 1",
  gpuModel: "NVIDIA A100",
  gpuCount: 4,
  status: "Available",
  activeFault: "None",
};

const runningNode: ComputeNode = {
  ...node,
  status: "Running",
};

const healthyHistory: HealthCheck[] = [
  {
    id: "health-1",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 60,
    gpuUsagePercent: 70,
    gpuTemperatureCelsius: 68,
    checkedAt: "2026-10-06T16:25:21.000Z",
  },
  {
    id: "health-2",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 61.5,
    gpuUsagePercent: 70.5,
    gpuTemperatureCelsius: 71,
    checkedAt: "2026-10-06T16:25:31.000Z",
  },
  {
    id: "health-3",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 63.4,
    gpuUsagePercent: 71.2,
    gpuTemperatureCelsius: 74,
    checkedAt: "2026-10-06T16:25:41.000Z",
  },
];

const unhealthyHistory: HealthCheck[] = [
  {
    id: "health-1",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: 80,
    gpuUsagePercent: 60,
    gpuTemperatureCelsius: 75,
    checkedAt: "2026-10-06T16:26:21.000Z",
  },
  {
    id: "health-2",
    computeNodeId: "node-1",
    isHealthy: false,
    cpuUsagePercent: 93.3,
    gpuUsagePercent: 63.1,
    gpuTemperatureCelsius: 78,
    checkedAt: "2026-10-06T16:26:41.000Z",
  },
];

const missingMetricsHistory: HealthCheck[] = [
  {
    id: "health-missing",
    computeNodeId: "node-1",
    isHealthy: true,
    cpuUsagePercent: null,
    gpuUsagePercent: null,
    gpuTemperatureCelsius: null,
    checkedAt: "2026-10-06T16:25:41.000Z",
  },
];

describe("NodeCard", () => {
  beforeEach(() => {
    jest.resetAllMocks();
  });

  it("renders node details", () => {
    renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
    );

    expect(
        screen.getByText("GPU Node 1"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("NVIDIA A100"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("4"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("Available"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("None"),
    ).toBeInTheDocument();
  });

  it("shows no health check message when health data is unavailable", () => {
    renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
    );

    expect(
        screen.getByText("Health"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("No health check yet"),
    ).toBeInTheDocument();
  });

  it("renders healthy health metrics and sparklines", () => {
    renderWithProviders(
        <NodeCard
            node={runningNode}
            healthHistory={healthyHistory}
        />,
    );

    expect(
        screen.getByText("Healthy"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("63.4%"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("74.0°C"),
    ).toBeInTheDocument();

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

  it("renders unhealthy health metrics", () => {
    renderWithProviders(
        <NodeCard
            node={{
              ...runningNode,
              status: "Unhealthy",
            }}
            healthHistory={unhealthyHistory}
        />,
    );

    expect(
        screen.getAllByText("Unhealthy"),
    ).toHaveLength(2);

    expect(
        screen.getByText("93.3%"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("78.0°C"),
    ).toBeInTheDocument();

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

  it("renders N/A for missing health metrics", () => {
    renderWithProviders(
        <NodeCard
            node={runningNode}
            healthHistory={missingMetricsHistory}
        />,
    );

    expect(
        screen.getAllByText("N/A"),
    ).toHaveLength(2);
  });

  it("starts an available node when Start is clicked", async () => {
    const user = userEvent.setup();

    const updatedNode: ComputeNode = {
      ...node,
      status: "Running",
    };

    mockedNodesApi.start.mockResolvedValue(
        updatedNode,
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
    );

    store.dispatch({
      type: "nodes/fetchNodes/fulfilled",
      payload: [node],
    });

    await user.click(
        screen.getByRole("button", {
          name: "Start",
        }),
    );

    await waitFor(() => {
      expect(
          mockedNodesApi.start,
      ).toHaveBeenCalledWith("node-1");

      expect(
          store.getState().nodes.nodes,
      ).toEqual([
        updatedNode,
      ]);
    });

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("stops a running node when Stop is clicked", async () => {
    const user = userEvent.setup();

    const updatedNode: ComputeNode = {
      ...runningNode,
      status: "Stopped",
    };

    mockedNodesApi.stop.mockResolvedValue(
        updatedNode,
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={runningNode}
            healthHistory={[]}
        />,
    );

    store.dispatch({
      type: "nodes/fetchNodes/fulfilled",
      payload: [runningNode],
    });

    await user.click(
        screen.getByRole("button", {
          name: "Stop",
        }),
    );

    await waitFor(() => {
      expect(
          mockedNodesApi.stop,
      ).toHaveBeenCalledWith("node-1");

      expect(
          store.getState().nodes.nodes,
      ).toEqual([
        updatedNode,
      ]);
    });

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("restarts a running node when Restart is clicked", async () => {
    const user = userEvent.setup();

    const updatedNode: ComputeNode = {
      ...runningNode,
      status: "Running",
    };

    mockedNodesApi.restart.mockResolvedValue(
        updatedNode,
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={runningNode}
            healthHistory={[]}
        />,
    );

    store.dispatch({
      type: "nodes/fetchNodes/fulfilled",
      payload: [runningNode],
    });

    await user.click(
        screen.getByRole("button", {
          name: "Restart",
        }),
    );

    await waitFor(() => {
      expect(
          mockedNodesApi.restart,
      ).toHaveBeenCalledWith("node-1");

      expect(
          store.getState().nodes.nodes,
      ).toEqual([
        updatedNode,
      ]);
    });

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("shows an error when starting a node fails", async () => {
    const user = userEvent.setup();

    mockedNodesApi.start.mockRejectedValue(
        new Error("Failed to start node"),
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
    );

    await user.click(
        screen.getByRole("button", {
          name: "Start",
        }),
    );

    await waitFor(() => {
      expect(
          store.getState().nodes.error,
      ).toBe("Failed to start node");
    });

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("shows an error when stopping a node fails", async () => {
    const user = userEvent.setup();

    mockedNodesApi.stop.mockRejectedValue(
        new Error("Failed to stop node"),
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={runningNode}
            healthHistory={[]}
        />,
    );

    await user.click(
        screen.getByRole("button", {
          name: "Stop",
        }),
    );

    await waitFor(() => {
      expect(
          store.getState().nodes.error,
      ).toBe("Failed to stop node");
    });

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("shows an error when restarting a node fails", async () => {
    const user = userEvent.setup();

    mockedNodesApi.restart.mockRejectedValue(
        new Error("Failed to restart node"),
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={runningNode}
            healthHistory={[]}
        />,
    );

    await user.click(
        screen.getByRole("button", {
          name: "Restart",
        }),
    );

    await waitFor(() => {
      expect(
          store.getState().nodes.error,
      ).toBe("Failed to restart node");
    });

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("shows a loading state while starting a node", async () => {
    const user = userEvent.setup();

    let resolveStart:
        | ((value: ComputeNode) => void)
        | undefined;

    mockedNodesApi.start.mockReturnValue(
        new Promise<ComputeNode>((resolve) => {
          resolveStart = resolve;
        }),
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
    );

    await user.click(
        screen.getByRole("button", {
          name: "Start",
        }),
    );

    expect(
        screen.getByRole("button", {
          name: "Starting...",
        }),
    ).toBeDisabled();

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(true);

    resolveStart!({
      ...node,
      status: "Running",
    });

    await waitFor(() => {
      expect(
          store.getState().nodes
              .actionLoadingByNodeId["node-1"],
      ).toBe(false);
    });
  });

  it("shows a loading state while stopping a node", async () => {
    const user = userEvent.setup();

    let resolveStop:
        | ((value: ComputeNode) => void)
        | undefined;

    mockedNodesApi.stop.mockReturnValue(
        new Promise<ComputeNode>((resolve) => {
          resolveStop = resolve;
        }),
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={runningNode}
            healthHistory={[]}
        />,
    );

    await user.click(
        screen.getByRole("button", {
          name: "Stop",
        }),
    );

    expect(
        screen.getByRole("button", {
          name: "Stopping...",
        }),
    ).toBeDisabled();

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(true);

    resolveStop!({
      ...runningNode,
      status: "Stopped",
    });

    await waitFor(() => {
      expect(
          store.getState().nodes
              .actionLoadingByNodeId["node-1"],
      ).toBe(false);
    });
  });

  it("shows a loading state while restarting a node", async () => {
    const user = userEvent.setup();

    let resolveRestart:
        | ((value: ComputeNode) => void)
        | undefined;

    mockedNodesApi.restart.mockReturnValue(
        new Promise<ComputeNode>((resolve) => {
          resolveRestart = resolve;
        }),
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={runningNode}
            healthHistory={[]}
        />,
    );

    await user.click(
        screen.getByRole("button", {
          name: "Restart",
        }),
    );

    expect(
        screen.getByRole("button", {
          name: "Restarting...",
        }),
    ).toBeDisabled();

    expect(
        store.getState().nodes
            .actionLoadingByNodeId["node-1"],
    ).toBe(true);

    resolveRestart!({
      ...runningNode,
      status: "Running",
    });

    await waitFor(() => {
      expect(
          store.getState().nodes
              .actionLoadingByNodeId["node-1"],
      ).toBe(false);
    });
  });

  it("does not delete a node when deletion is cancelled", async () => {
    const user = userEvent.setup();

    const confirmSpy = jest
        .spyOn(window, "confirm")
        .mockReturnValue(false);

    const { store } = renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
        {
          nodes: {
            nodes: [node],
          },
        },
    );

    await user.click(
        screen.getByRole("button", {
          name: "Delete",
        }),
    );

    expect(
        confirmSpy,
    ).toHaveBeenCalledWith(
        'Are you sure you want to delete "GPU Node 1"?',
    );

    expect(
        mockedNodesApi.delete,
    ).not.toHaveBeenCalled();

    expect(
        store.getState().nodes.nodes,
    ).toEqual([node]);

    confirmSpy.mockRestore();
  });

  it("deletes a node when deletion is confirmed", async () => {
    const user = userEvent.setup();

    jest
        .spyOn(window, "confirm")
        .mockReturnValue(true);

    mockedNodesApi.delete.mockResolvedValue(
        undefined,
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
        {
          nodes: {
            nodes: [node],
          },
        },
    );

    await user.click(
        screen.getByRole("button", {
          name: "Delete",
        }),
    );

    await waitFor(() => {
      expect(
          mockedNodesApi.delete,
      ).toHaveBeenCalledWith("node-1");
    });

    await waitFor(() => {
      expect(
          store.getState().nodes.nodes,
      ).toEqual([]);
    });

    expect(
        store.getState().nodes.deletingNodeId,
    ).toBeNull();
  });

  it("shows a loading state while deleting a node", async () => {
    const user = userEvent.setup();

    jest
        .spyOn(window, "confirm")
        .mockReturnValue(true);

    let resolveDelete:
        | (() => void)
        | undefined;

    mockedNodesApi.delete.mockReturnValue(
        new Promise<void>((resolve) => {
          resolveDelete = resolve;
        }),
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
        {
          nodes: {
            nodes: [node],
          },
        },
    );

    await user.click(
        screen.getByRole("button", {
          name: "Delete",
        }),
    );

    expect(
        screen.getByRole("button", {
          name: "Deleting...",
        }),
    ).toBeDisabled();

    expect(
        store.getState().nodes.deletingNodeId,
    ).toBe("node-1");

    resolveDelete!();

    await waitFor(() => {
      expect(
          store.getState().nodes.deletingNodeId,
      ).toBeNull();
    });
  });

  it("shows an error when deleting a node fails", async () => {
    const user = userEvent.setup();

    jest
        .spyOn(window, "confirm")
        .mockReturnValue(true);

    mockedNodesApi.delete.mockRejectedValue(
        new Error("Failed to delete node"),
    );

    const { store } = renderWithProviders(
        <NodeCard
            node={node}
            healthHistory={[]}
        />,
        {
          nodes: {
            nodes: [node],
          },
        },
    );

    await user.click(
        screen.getByRole("button", {
          name: "Delete",
        }),
    );

    await waitFor(() => {
      expect(
          store.getState().nodes
              .deleteErrorByNodeId["node-1"],
      ).toBe("Failed to delete node");
    });

    expect(
        screen.getByRole("alert"),
    ).toHaveTextContent(
        "Failed to delete node",
    );

    expect(
        store.getState().nodes.deletingNodeId,
    ).toBeNull();

    expect(
        store.getState().nodes.nodes,
    ).toEqual([node]);
  });
});