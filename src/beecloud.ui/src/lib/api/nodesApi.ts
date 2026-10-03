import { apiClient } from "./client";
import type { ComputeNode } from "./models/computeNode";
import type { CreateComputeNodeRequest } from "./models/createComputeNodeRequest";

export const nodesApi = {
  getAll(): Promise<ComputeNode[]> {
    return apiClient.get<ComputeNode[]>("/api/v1/nodes");
  },

  getById(id: string): Promise<ComputeNode> {
    return apiClient.get<ComputeNode>(`/api/v1/nodes/${id}`);
  },

  start(id: string): Promise<ComputeNode> {
    return apiClient.post<ComputeNode, undefined>(`/api/v1/nodes/${id}/start`, undefined);
  },

  stop(id: string): Promise<ComputeNode> {
    return apiClient.post<ComputeNode, undefined>(`/api/v1/nodes/${id}/stop`, undefined);
  },

  restart(id: string): Promise<ComputeNode> {
    return apiClient.post<ComputeNode, undefined>(`/api/v1/nodes/${id}/restart`, undefined);
  },

  create(request: CreateComputeNodeRequest): Promise<ComputeNode> {
    return apiClient.post<ComputeNode, CreateComputeNodeRequest>("/api/v1/nodes", request);
  },
};
