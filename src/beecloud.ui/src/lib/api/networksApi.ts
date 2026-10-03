import { apiClient } from "./client";
import { CreateNetworkRequest, Network, NetworkAttachment } from "@/types/network";

export const networksApi = {
  getAll(): Promise<Network[]> {
    return apiClient.get<Network[]>("/api/v1/networks");
  },

  getById(id: string): Promise<Network> {
    return apiClient.get<Network>(`/api/v1/networks/${id}`);
  },

  create(request: CreateNetworkRequest): Promise<Network> {
    return apiClient.post<Network, CreateNetworkRequest>("/api/v1/networks", request);
  },

  activate(id: string): Promise<void> {
    return apiClient.post<void, undefined>(`/api/v1/networks/${id}/activate`, undefined);
  },

  deactivate(id: string): Promise<void> {
    return apiClient.post<void, undefined>(`/api/v1/networks/${id}/deactivate`, undefined);
  },

  getNetworkNodes(networkId: string): Promise<NetworkAttachment[]> {
    return apiClient.get<NetworkAttachment[]>(`/api/v1/networks/${networkId}/nodes`);
  },

  getNodeNetworks(nodeId: string): Promise<NetworkAttachment[]> {
    return apiClient.get<NetworkAttachment[]>(`/api/v1/nodes/${nodeId}/networks`);
  },

  attach(nodeId: string, networkId: string): Promise<NetworkAttachment> {
    return apiClient.post<NetworkAttachment, undefined>(
      `/api/v1/nodes/${nodeId}/networks/${networkId}`,
      undefined,
    );
  },

  detach(nodeId: string, networkId: string): Promise<void> {
    return apiClient.delete(`/api/v1/nodes/${nodeId}/networks/${networkId}`);
  },
};
