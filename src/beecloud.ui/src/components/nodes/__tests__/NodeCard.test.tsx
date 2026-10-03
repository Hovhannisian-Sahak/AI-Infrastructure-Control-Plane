
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test-utils";
import NodeCard from "@/components/nodes/NodeCard";
import { nodesApi } from "@/lib/api/nodesApi";
import type { ComputeNode } from "@/lib/api/models/computeNode";

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

describe("NodeCard", () => {
  beforeEach(() => {
    jest.resetAllMocks();
  });

  it("renders node details", () => {
    renderWithProviders(<NodeCard node={node} />);

    expect(screen.getByText("GPU Node 1")).toBeInTheDocument();
    expect(screen.getByText("NVIDIA A100")).toBeInTheDocument();
    expect(screen.getByText("4")).toBeInTheDocument();
    expect(screen.getByText("Available")).toBeInTheDocument();
    expect(screen.getByText("None")).toBeInTheDocument();
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
        <NodeCard node={node} />,
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
        <NodeCard node={node} />,
        {
          nodes: {
            nodes: [node],
          },
        },
    );

    await user.click(
        screen.getByRole("button", {
          name: "Start",
        }),
    );

    await waitFor(() => {
      expect(
          mockedNodesApi.start,
      ).toHaveBeenCalledWith("node-1");
    });

    await waitFor(() => {
      expect(
          store.getState().nodes.nodes,
      ).toEqual([updatedNode]);
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

    mockedNodesApi.stop.mockResolvedValue(updatedNode);

    const { store } = renderWithProviders(
        <NodeCard node={runningNode} />,
    );

    // Seed the test store with the node being acted on.
    store.dispatch({
      type: "nodes/fetchNodes/fulfilled",
      payload: [runningNode],
    });

    await user.click(
        screen.getByRole("button", { name: "Stop" }),
    );

    await waitFor(() => {
      expect(mockedNodesApi.stop).toHaveBeenCalledWith("node-1");
      expect(store.getState().nodes.nodes).toEqual([
        updatedNode,
      ]);
    });

    expect(
        store.getState().nodes.actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("restarts a running node when Restart is clicked", async () => {
    const user = userEvent.setup();
    const updatedNode: ComputeNode = {
      ...runningNode,
      status: "Running",
    };

    mockedNodesApi.restart.mockResolvedValue(updatedNode);

    const { store } = renderWithProviders(
        <NodeCard node={runningNode} />,
    );

    store.dispatch({
      type: "nodes/fetchNodes/fulfilled",
      payload: [runningNode],
    });

    await user.click(
        screen.getByRole("button", { name: "Restart" }),
    );

    await waitFor(() => {
      expect(mockedNodesApi.restart).toHaveBeenCalledWith("node-1");
      expect(store.getState().nodes.nodes).toEqual([
        updatedNode,
      ]);
    });

    expect(
        store.getState().nodes.actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("shows an error when starting a node fails", async () => {
    const user = userEvent.setup();

    mockedNodesApi.start.mockRejectedValue(
        new Error("Failed to start node"),
    );

    const { store } = renderWithProviders(
        <NodeCard node={node} />,
    );

    await user.click(
        screen.getByRole("button", { name: "Start" }),
    );

    await waitFor(() => {
      expect(store.getState().nodes.error).toBe(
          "Failed to start node",
      );
    });

    expect(
        store.getState().nodes.actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("shows an error when stopping a node fails", async () => {
    const user = userEvent.setup();

    mockedNodesApi.stop.mockRejectedValue(
        new Error("Failed to stop node"),
    );

    const { store } = renderWithProviders(
        <NodeCard node={runningNode} />,
    );

    await user.click(
        screen.getByRole("button", { name: "Stop" }),
    );

    await waitFor(() => {
      expect(store.getState().nodes.error).toBe(
          "Failed to stop node",
      );
    });

    expect(
        store.getState().nodes.actionLoadingByNodeId["node-1"],
    ).toBe(false);
  });

  it("shows an error when restarting a node fails", async () => {
    const user = userEvent.setup();

    mockedNodesApi.restart.mockRejectedValue(
        new Error("Failed to restart node"),
    );

    const { store } = renderWithProviders(
        <NodeCard node={runningNode} />,
    );

    await user.click(
        screen.getByRole("button", { name: "Restart" }),
    );

    await waitFor(() => {
      expect(store.getState().nodes.error).toBe(
          "Failed to restart node",
      );
    });

    expect(
        store.getState().nodes.actionLoadingByNodeId["node-1"],
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
        <NodeCard node={node} />,
    );

    await user.click(
        screen.getByRole("button", { name: "Start" }),
    );

    expect(
        screen.getByRole("button", { name: "Starting..." }),
    ).toBeDisabled();

    expect(
        store.getState().nodes.actionLoadingByNodeId["node-1"],
    ).toBe(true);

    resolveStart!({
      ...node,
      status: "Running",
    });

    await waitFor(() => {
      expect(
          store.getState().nodes.actionLoadingByNodeId["node-1"],
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
        <NodeCard node={runningNode} />,
    );

    await user.click(
        screen.getByRole("button", { name: "Stop" }),
    );

    expect(
        screen.getByRole("button", { name: "Stopping..." }),
    ).toBeDisabled();

    expect(
        store.getState().nodes.actionLoadingByNodeId["node-1"],
    ).toBe(true);

    resolveStop!({
      ...runningNode,
      status: "Stopped",
    });

    await waitFor(() => {
      expect(
          store.getState().nodes.actionLoadingByNodeId["node-1"],
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
        <NodeCard node={runningNode} />,
    );

    await user.click(
        screen.getByRole("button", { name: "Restart" }),
    );

    expect(
        screen.getByRole("button", { name: "Restarting..." }),
    ).toBeDisabled();

    expect(
        store.getState().nodes.actionLoadingByNodeId["node-1"],
    ).toBe(true);

    resolveRestart!({
      ...runningNode,
      status: "Running",
    });

    await waitFor(() => {
      expect(
          store.getState().nodes.actionLoadingByNodeId["node-1"],
      ).toBe(false);
    });
  });
});