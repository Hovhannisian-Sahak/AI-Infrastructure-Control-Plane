import { apiClient } from "./client";
import type { ComputeNode } from "./models/computeNode";

export const nodesApi = {
    getAll(): Promise<ComputeNode[]> {
        return apiClient.get<ComputeNode[]>("/api/v1/nodes");
    },

    getById(id: string): Promise<ComputeNode> {
        return apiClient.get<ComputeNode>(`/api/v1/nodes/${id}`);
    },

    start(id: string): Promise<ComputeNode> {
        return apiClient.post<ComputeNode, undefined>(
            `/api/v1/nodes/${id}/start`,
            undefined,
        );
    },

    stop(id: string): Promise<ComputeNode> {
        return apiClient.post<ComputeNode, undefined>(
            `/api/v1/nodes/${id}/stop`,
            undefined,
        );
    },
};