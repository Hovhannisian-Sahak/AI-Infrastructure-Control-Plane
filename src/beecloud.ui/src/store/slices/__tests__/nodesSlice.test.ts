import { configureStore } from "@reduxjs/toolkit";
import nodesReducer, { fetchNodes, createNode, startNode, stopNode, restartNode, deleteNode } from "../nodesSlice";
import { nodesApi } from "@/lib/api/nodesApi";

jest.mock("@/lib/api/nodesApi");

const mockedNodesApi = jest.mocked(nodesApi);

describe("nodesSlice", () => {
  it("stores nodes when fetchNodes succeeds", async () => {
    const nodes = [
      {
        id: "node-1",
        name: "GPU Node 1",
        gpuModel: "NVIDIA A100",
        gpuCount: 4,
        status: "Available" as const,
        activeFault: "None" as const,
      },
    ];

    mockedNodesApi.getAll.mockResolvedValue(nodes);

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    await store.dispatch(fetchNodes());

    const state = store.getState().nodes;

    expect(state.loading).toBe(false);
    expect(state.error).toBeNull();
    expect(state.nodes).toEqual(nodes);
  });

  it("stores an error when fetchNodes fails", async () => {
    mockedNodesApi.getAll.mockRejectedValue(new Error("API unavailable"));

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    await store.dispatch(fetchNodes());

    const state = store.getState().nodes;

    expect(state.loading).toBe(false);
    expect(state.error).toBe("API unavailable");
    expect(state.nodes).toEqual([]);
  });

  it("refreshes node state when a restart request fails", async () => {
    mockedNodesApi.getAll.mockClear();
    const refreshedNode = {
      id: "node-1",
      name: "GPU Node 1",
      gpuModel: "NVIDIA A100",
      gpuCount: 4,
      status: "Quarantined" as const,
      activeFault: "GpuOverheat" as const,
    };
    mockedNodesApi.restart.mockRejectedValue(
      new Error("Invalid node state transition: Quarantined -> Stopping."),
    );
    mockedNodesApi.getAll.mockResolvedValue([refreshedNode]);

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    await store.dispatch(restartNode(refreshedNode.id));

    expect(mockedNodesApi.getAll).toHaveBeenCalledTimes(1);
    expect(store.getState().nodes.nodes).toEqual([refreshedNode]);
    expect(store.getState().nodes.error)
      .toBe("Invalid node state transition: Quarantined -> Stopping.");
  });

  it("ignores an older fetch response after a newer refresh completes", async () => {
    let resolveFirst!: (nodes: Awaited<ReturnType<typeof nodesApi.getAll>>) => void;
    let resolveSecond!: (nodes: Awaited<ReturnType<typeof nodesApi.getAll>>) => void;
    mockedNodesApi.getAll
      .mockReturnValueOnce(new Promise(resolve => { resolveFirst = resolve; }))
      .mockReturnValueOnce(new Promise(resolve => { resolveSecond = resolve; }));

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });
    const olderFetch = store.dispatch(fetchNodes());
    const newerFetch = store.dispatch(fetchNodes());
    const latestNodes = [{
      id: "latest-node",
      name: "Latest node",
      gpuModel: "NVIDIA H100",
      gpuCount: 1,
      status: "Available" as const,
      activeFault: "None" as const,
    }];

    resolveSecond(latestNodes);
    await newerFetch;
    resolveFirst([]);
    await olderFetch;

    expect(store.getState().nodes.nodes).toEqual(latestNodes);
  });

  it("keeps newest nodes first when a refresh returns creation order", async () => {
    const olderNode = {
      id: "node-older",
      name: "Older node",
      gpuModel: "NVIDIA A100",
      gpuCount: 1,
      status: "Available" as const,
      activeFault: "None" as const,
      createdAt: "2026-10-08T10:00:00Z",
    };
    const newlyCreatedNode = {
      id: "node-newer",
      name: "Newly created node",
      gpuModel: "NVIDIA H100",
      gpuCount: 1,
      status: "Available" as const,
      activeFault: "None" as const,
      createdAt: "2026-10-08T10:01:00Z",
    };
    mockedNodesApi.getAll.mockResolvedValue([
      olderNode,
      newlyCreatedNode,
    ]);

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    await store.dispatch(fetchNodes());

    expect(store.getState().nodes.nodes).toEqual([
      newlyCreatedNode,
      olderNode,
    ]);
  });

  it("adds a node when createNode succeeds", async () => {
    const request = {
      name: "GPU Node 2",
      gpuModel: "NVIDIA H100",
      gpuCount: 8,
    };

    const createdNode = {
      id: "node-2",
      name: "GPU Node 2",
      gpuModel: "NVIDIA H100",
      gpuCount: 8,
      status: "Provisioning" as const,
      activeFault: "None" as const,
    };

    mockedNodesApi.create.mockResolvedValue(createdNode);

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });
    const existingNode = {
      id: "node-1",
      name: "GPU Node 1",
      gpuModel: "NVIDIA A100",
      gpuCount: 4,
      status: "Available" as const,
      activeFault: "None" as const,
    };
    store.dispatch(fetchNodes.pending("test-request", undefined));
    store.dispatch(fetchNodes.fulfilled([existingNode], "test-request", undefined));

    await store.dispatch(createNode(request));

    const state = store.getState().nodes;

    expect(store.getState().nodes.createSuccess).toBe("Node created successfully.");
    expect(state.loading).toBe(false);
    expect(state.error).toBeNull();
    expect(state.nodes).toEqual([createdNode, existingNode]);
  });

  it("stores an error when createNode fails", async () => {
    mockedNodesApi.create.mockRejectedValue(new Error("Node name already exists"));

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    await store.dispatch(
      createNode({
        name: "GPU Node 2",
        gpuModel: "NVIDIA H100",
        gpuCount: 8,
      }),
    );

    const state = store.getState().nodes;

    expect(state.loading).toBe(false);
    expect(state.error).toBe("Node name already exists");
    expect(state.createSuccess).toBeNull();
    expect(state.nodes).toEqual([]);
  });
  it("tracks action loading state when starting a node", async () => {
    const node = {
      id: "node-1",
      name: "GPU Node 1",
      gpuModel: "NVIDIA A100",
      gpuCount: 4,
      status: "Available" as const,
      activeFault: "None" as const,
    };

    mockedNodesApi.start.mockResolvedValue({
      ...node,
      status: "Running",
    });

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    store.dispatch(fetchNodes.pending("test-request", undefined));
    store.dispatch(fetchNodes.fulfilled([node], "test-request", undefined));

    const promise = store.dispatch(startNode(node.id));

    expect(store.getState().nodes.actionLoadingByNodeId["node-1"]).toBe(true);

    await promise;

    const state = store.getState().nodes;

    expect(state.actionLoadingByNodeId["node-1"]).toBe(false);

    expect(state.nodes).toEqual([
      {
        ...node,
        status: "Running",
      },
    ]);
  });
  it("tracks action loading state when stopping a node", async () => {
    const node = {
      id: "node-1",
      name: "GPU Node 1",
      gpuModel: "NVIDIA A100",
      gpuCount: 4,
      status: "Running" as const,
      activeFault: "None" as const,
    };

    mockedNodesApi.stop.mockResolvedValue({
      ...node,
      status: "Stopped",
    });

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    store.dispatch(fetchNodes.pending("test-request", undefined));
    store.dispatch(fetchNodes.fulfilled([node], "test-request", undefined));

    const promise = store.dispatch(stopNode(node.id));

    expect(store.getState().nodes.actionLoadingByNodeId["node-1"]).toBe(true);

    await promise;

    const state = store.getState().nodes;

    expect(state.actionLoadingByNodeId["node-1"]).toBe(false);

    expect(state.nodes).toEqual([
      {
        ...node,
        status: "Stopped",
      },
    ]);
  });
  it("tracks loading state when creating a node", async () => {
    let resolveCreate: (value: {
      id: string;
      name: string;
      gpuModel: string;
      gpuCount: number;
      status: "Provisioning";
      activeFault: "None";
    }) => void;

    const createPromise = new Promise<{
      id: string;
      name: string;
      gpuModel: string;
      gpuCount: number;
      status: "Provisioning";
      activeFault: "None";
    }>((resolve) => {
      resolveCreate = resolve;
    });

    mockedNodesApi.create.mockReturnValue(createPromise);

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    const promise = store.dispatch(
      createNode({
        name: "GPU Node 2",
        gpuModel: "NVIDIA H100",
        gpuCount: 8,
      }),
    );

    expect(store.getState().nodes.creating).toBe(true);

    resolveCreate!({
      id: "node-2",
      name: "GPU Node 2",
      gpuModel: "NVIDIA H100",
      gpuCount: 8,
      status: "Provisioning",
      activeFault: "None",
    });

    await promise;

    expect(store.getState().nodes.creating).toBe(false);
  });
  it("removes a node when deleteNode succeeds", async () => {
    const node = {
      id: "node-1",
      name: "GPU Node 1",
      gpuModel: "NVIDIA A100",
      gpuCount: 4,
      status: "Available" as const,
      activeFault: "None" as const,
    };

    mockedNodesApi.delete.mockResolvedValue(undefined);

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    store.dispatch(fetchNodes.pending("test-request", undefined));
    store.dispatch(fetchNodes.fulfilled([node], "test-request", undefined));

    await store.dispatch(deleteNode(node.id));

    const state = store.getState().nodes;

    expect(mockedNodesApi.delete).toHaveBeenCalledWith("node-1");
    expect(state.nodes).toEqual([]);
    expect(state.deletingNodeId).toBeNull();
    expect(state.deleteErrorByNodeId["node-1"]).toBeUndefined();
  });
  it("stores a delete error when deleteNode fails", async () => {
    mockedNodesApi.delete.mockRejectedValue(
        new Error("Failed to delete node"),
    );

    const store = configureStore({
      reducer: {
        nodes: nodesReducer,
      },
    });

    const node = {
      id: "node-1",
      name: "GPU Node 1",
      gpuModel: "NVIDIA A100",
      gpuCount: 4,
      status: "Available" as const,
      activeFault: "None" as const,
    };

    store.dispatch(fetchNodes.pending("test-request", undefined));
    store.dispatch(fetchNodes.fulfilled([node], "test-request", undefined));

    await store.dispatch(deleteNode(node.id));

    const state = store.getState().nodes;

    expect(state.nodes).toEqual([node]);
    expect(state.deletingNodeId).toBeNull();
    expect(state.deleteErrorByNodeId["node-1"]).toBe(
        "Failed to delete node",
    );
  });
});
