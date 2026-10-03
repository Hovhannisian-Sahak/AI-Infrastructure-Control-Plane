export type Network = {
    id: string;
    name: string;
    description: string | null;
    isActive: boolean;
    maxAttachments: number;
    createdAt: string;
};

export type NetworkAttachment = {
    id: string;
    computeNodeId: string;
    networkId: string;
    attachedAt: string;
};

export type CreateNetworkRequest = {
    name: string;
    description?: string;
};