import { apiClient } from "../client";
import { nodesApi } from "../nodesApi";
import type { HealthCheck } from "../models/healthCheck";
import type { NodeMetric } from "../models/nodeMetric";

jest.mock("../client", () => ({
  apiClient: {
    get: jest.fn(),
    post: jest.fn(),
    delete: jest.fn(),
  },
}));

const mockedApiClient = jest.mocked(apiClient);

describe("nodesApi", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });
  it("creates a node", async () => {
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

    mockedApiClient.post.mockResolvedValue(createdNode);

    const result = await nodesApi.create(request);

    expect(result).toEqual(createdNode);

    expect(mockedApiClient.post).toHaveBeenCalledWith("/api/v1/nodes", request);
  });
  it("gets all nodes", async () => {
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

    mockedApiClient.get.mockResolvedValue(nodes);

    const result = await nodesApi.getAll();

    expect(mockedApiClient.get).toHaveBeenCalledWith("/api/v1/nodes");

    expect(result).toEqual(nodes);
  });

  it("starts a node", async () => {
    const node = {
      id: "node-1",
      name: "GPU Node 1",
      gpuModel: "NVIDIA A100",
      gpuCount: 4,
      status: "Running" as const,
      activeFault: "None" as const,
    };

    mockedApiClient.post.mockResolvedValue(node);

    const result = await nodesApi.start("node-1");

    expect(mockedApiClient.post).toHaveBeenCalledWith("/api/v1/nodes/node-1/start", undefined);

    expect(result).toEqual(node);
  });

  it("stops a node", async () => {
    const node = {
      id: "node-1",
      name: "GPU Node 1",
      gpuModel: "NVIDIA A100",
      gpuCount: 4,
      status: "Stopped" as const,
      activeFault: "None" as const,
    };

    mockedApiClient.post.mockResolvedValue(node);

    const result = await nodesApi.stop("node-1");

    expect(mockedApiClient.post).toHaveBeenCalledWith("/api/v1/nodes/node-1/stop", undefined);

    expect(result).toEqual(node);
  });

  it("gets health history with a time range and limit", async () => {
    const history: HealthCheck[] = [];
    const from = "2026-10-07T10:00:00.000Z";
    const to = "2026-10-07T14:00:00.000Z";

    mockedApiClient.get.mockResolvedValue(history);

    const result = await nodesApi.getHealthHistory(
      "node-1",
      100,
      from,
      to,
    );

    expect(mockedApiClient.get).toHaveBeenCalledWith(
      "/api/v1/nodes/node-1/health/history?from=2026-10-07T10%3A00%3A00.000Z&to=2026-10-07T14%3A00%3A00.000Z&limit=100",
    );
    expect(result).toBe(history);
  });

  it("gets health history with only the limit when no time range is provided", async () => {
    mockedApiClient.get.mockResolvedValue([]);

    await nodesApi.getHealthHistory("node-1");

    expect(mockedApiClient.get).toHaveBeenCalledWith(
      "/api/v1/nodes/node-1/health/history?limit=10",
    );
  });

  it("gets node metrics with backend time filters and limit", async () => {
    const metrics: NodeMetric[] = [];
    mockedApiClient.get.mockResolvedValue(metrics);

    const result = await nodesApi.getNodeMetrics(
      "node-1",
      100,
      "2026-10-07T10:00:00.000Z",
      "2026-10-07T14:00:00.000Z",
    );

    expect(mockedApiClient.get).toHaveBeenCalledWith(
      "/api/v1/nodes/node-1/metrics?from=2026-10-07T10%3A00%3A00.000Z&to=2026-10-07T14%3A00%3A00.000Z&limit=100",
    );
    expect(result).toBe(metrics);
  });

  it("propagates an error when starting a node fails", async () => {
    const error = new Error("POST /api/v1/nodes/node-1/start failed");

    mockedApiClient.post.mockRejectedValue(error);

    await expect(nodesApi.start("node-1")).rejects.toThrow(
      "POST /api/v1/nodes/node-1/start failed",
    );

    expect(mockedApiClient.post).toHaveBeenCalledWith("/api/v1/nodes/node-1/start", undefined);
  });

  it("propagates an error when stopping a node fails", async () => {
    const error = new Error("POST /api/v1/nodes/node-1/stop failed");

    mockedApiClient.post.mockRejectedValue(error);

    await expect(nodesApi.stop("node-1")).rejects.toThrow("POST /api/v1/nodes/node-1/stop failed");

    expect(mockedApiClient.post).toHaveBeenCalledWith("/api/v1/nodes/node-1/stop", undefined);
  });
  it("deletes a node", async () => {
    mockedApiClient.delete.mockResolvedValue(undefined);

    await nodesApi.delete("node-1");

    expect(mockedApiClient.delete).toHaveBeenCalledWith(
        "/api/v1/nodes/node-1",
    );
  });
});
