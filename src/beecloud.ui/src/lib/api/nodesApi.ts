import { apiClient } from "./client";
import type { ComputeNode } from "./models/computeNode";
import type { CreateComputeNodeRequest } from "./models/createComputeNodeRequest";
import type { HealthCheck } from "./models/healthCheck";
import type { NodeMetric } from "./models/nodeMetric";
import type { CursorPageResponse } from "./models/pageResponse";

type HistoryPageParams = {
  from?: string;
  to?: string;
  cursor?: string | null;
  previous?: boolean;
  limit?: number;
};

export const nodesApi = {
  getAll(): Promise<ComputeNode[]> {
    return apiClient.get<ComputeNode[]>(
        "/api/v1/nodes",
    );
  },

  getById(id: string): Promise<ComputeNode> {
    return apiClient.get<ComputeNode>(
        `/api/v1/nodes/${id}`,
    );
  },

  start(id: string): Promise<ComputeNode> {
    return apiClient.post<
        ComputeNode,
        undefined
    >(
        `/api/v1/nodes/${id}/start`,
        undefined,
    );
  },

  stop(id: string): Promise<ComputeNode> {
    return apiClient.post<
        ComputeNode,
        undefined
    >(
        `/api/v1/nodes/${id}/stop`,
        undefined,
    );
  },

  restart(id: string): Promise<ComputeNode> {
    return apiClient.post<
        ComputeNode,
        undefined
    >(
        `/api/v1/nodes/${id}/restart`,
        undefined,
    );
  },

  create(
      request: CreateComputeNodeRequest,
  ): Promise<ComputeNode> {
    return apiClient.post<
        ComputeNode,
        CreateComputeNodeRequest
    >(
        "/api/v1/nodes",
        request,
    );
  },

  delete(id: string): Promise<void> {
    return apiClient.delete(
        `/api/v1/nodes/${id}`,
    );
  },

  getHealthHistory(
      id: string,
      limit = 10,
      from?: string,
      to?: string,
  ): Promise<HealthCheck[]> {
    const query = new URLSearchParams();

    if (from) {
      query.set("from", from);
    }

    if (to) {
      query.set("to", to);
    }

    query.set("limit", String(limit));

    return apiClient.get<HealthCheck[]>(
        `/api/v1/nodes/${id}/health/history?${query.toString()}`,
    );
  },

  getHealthHistoryPage(
      id: string,
      params: HistoryPageParams,
  ): Promise<CursorPageResponse<HealthCheck>> {
    const query = new URLSearchParams();
    if (params.from) query.set("from", params.from);
    if (params.to) query.set("to", params.to);
    if (params.cursor) query.set("cursor", params.cursor);
    if (params.previous) query.set("previous", "true");
    query.set("limit", String(params.limit ?? 25));

    return apiClient.get<CursorPageResponse<HealthCheck>>(
        `/api/v1/nodes/${id}/health/history/page?${query.toString()}`,
    );
  },

  getLatestHealth(
      id: string,
  ): Promise<HealthCheck | null> {
    return apiClient.get<HealthCheck>(
        `/api/v1/nodes/${id}/health`,
    );
  },

  getNodeMetrics(
      id: string,
      limit = 100,
      from?: string,
      to?: string,
  ): Promise<NodeMetric[]> {
    const query = new URLSearchParams();

    if (from) {
      query.set("from", from);
    }

    if (to) {
      query.set("to", to);
    }

    query.set("limit", String(limit));

    return apiClient.get<NodeMetric[]>(
        `/api/v1/nodes/${id}/metrics?${query.toString()}`,
    );
  },

  getNodeMetricsPage(
      id: string,
      params: HistoryPageParams,
  ): Promise<CursorPageResponse<NodeMetric>> {
    const query = new URLSearchParams();
    if (params.from) query.set("from", params.from);
    if (params.to) query.set("to", params.to);
    if (params.cursor) query.set("cursor", params.cursor);
    if (params.previous) query.set("previous", "true");
    query.set("limit", String(params.limit ?? 25));

    return apiClient.get<CursorPageResponse<NodeMetric>>(
        `/api/v1/nodes/${id}/metrics/page?${query.toString()}`,
    );
  },
};