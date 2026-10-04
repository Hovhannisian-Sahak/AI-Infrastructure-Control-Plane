import { networksApi } from "../networksApi";
import { apiClient } from "../client";
import type {
    CreateNetworkRequest,
    Network,
    NetworkAttachment,
} from "@/types/network";

jest.mock("../client", () => ({
    apiClient: {
        get: jest.fn(),
        post: jest.fn(),
        delete: jest.fn(),
    },
}));

const mockedApiClient = jest.mocked(apiClient);

describe("networksApi", () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it("gets all networks", async () => {
        const networks: Network[] = [
            {
                id: "network-1",
                name: "Network 1",
                description: "Test network",
                isActive: true,
                maxAttachments: 4,
                createdAt: "2026-10-03T10:00:00Z",
            },
        ];

        mockedApiClient.get.mockResolvedValue(networks);

        const result = await networksApi.getAll();

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/networks",
        );
        expect(result).toEqual(networks);
    });

    it("gets a network by id", async () => {
        const network: Network = {
            id: "network-1",
            name: "Network 1",
            description: null,
            isActive: true,
            maxAttachments: 4,
            createdAt: "2026-10-03T10:00:00Z",
        };

        mockedApiClient.get.mockResolvedValue(network);

        const result = await networksApi.getById("network-1");

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/networks/network-1",
        );
        expect(result).toEqual(network);
    });

    it("creates a network", async () => {
        const request: CreateNetworkRequest = {
            name: "Production Network",
            description: "Production traffic",
        };

        const network: Network = {
            id: "network-1",
            name: "Production Network",
            description: "Production traffic",
            isActive: true,
            maxAttachments: 4,
            createdAt: "2026-10-03T10:00:00Z",
        };

        mockedApiClient.post.mockResolvedValue(network);

        const result = await networksApi.create(request);

        expect(mockedApiClient.post).toHaveBeenCalledWith(
            "/api/v1/networks",
            request,
        );
        expect(result).toEqual(network);
    });

    it("activates a network", async () => {
        mockedApiClient.post.mockResolvedValue(undefined);

        await networksApi.activate("network-1");

        expect(mockedApiClient.post).toHaveBeenCalledWith(
            "/api/v1/networks/network-1/activate",
            undefined,
        );
    });

    it("deactivates a network", async () => {
        mockedApiClient.post.mockResolvedValue(undefined);

        await networksApi.deactivate("network-1");

        expect(mockedApiClient.post).toHaveBeenCalledWith(
            "/api/v1/networks/network-1/deactivate",
            undefined,
        );
    });

    it("gets nodes attached to a network", async () => {
        const attachments: NetworkAttachment[] = [
            {
                id: "attachment-1",
                computeNodeId: "node-1",
                networkId: "network-1",
                attachedAt: "2026-10-03T10:00:00Z",
            },
        ];

        mockedApiClient.get.mockResolvedValue(attachments);

        const result =
            await networksApi.getNetworkNodes("network-1");

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/networks/network-1/nodes",
        );
        expect(result).toEqual(attachments);
    });

    it("gets networks attached to a node", async () => {
        const attachments: NetworkAttachment[] = [
            {
                id: "attachment-1",
                computeNodeId: "node-1",
                networkId: "network-1",
                attachedAt: "2026-10-03T10:00:00Z",
            },
        ];

        mockedApiClient.get.mockResolvedValue(attachments);

        const result =
            await networksApi.getNodeNetworks("node-1");

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/nodes/node-1/networks",
        );
        expect(result).toEqual(attachments);
    });

    it("attaches a node to a network", async () => {
        const attachment: NetworkAttachment = {
            id: "attachment-1",
            computeNodeId: "node-1",
            networkId: "network-1",
            attachedAt: "2026-10-03T10:00:00Z",
        };

        mockedApiClient.post.mockResolvedValue(attachment);

        const result = await networksApi.attach(
            "node-1",
            "network-1",
        );

        expect(mockedApiClient.post).toHaveBeenCalledWith(
            "/api/v1/nodes/node-1/networks/network-1",
            undefined,
        );
        expect(result).toEqual(attachment);
    });

    it("detaches a node from a network", async () => {
        mockedApiClient.delete.mockResolvedValue(undefined);

        await networksApi.detach(
            "node-1",
            "network-1",
        );

        expect(mockedApiClient.delete).toHaveBeenCalledWith(
            "/api/v1/nodes/node-1/networks/network-1",
        );
    });

    it("deletes a network", async () => {
        mockedApiClient.delete.mockResolvedValue(undefined);

        await networksApi.delete("network-1");

        expect(mockedApiClient.delete).toHaveBeenCalledWith(
            "/api/v1/networks/network-1",
        );
    });
});