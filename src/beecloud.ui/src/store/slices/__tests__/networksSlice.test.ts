import { configureStore } from "@reduxjs/toolkit";
import networksReducer, {
    activateNetwork,
    attachNodeToNetwork,
    createNetwork,
    deactivateNetwork,
    deleteNetwork,
    detachNodeFromNetwork,
    fetchNetworkAttachments,
    fetchNetworks,
} from "../networksSlice";
import { networksApi } from "@/lib/api/networksApi";
import type {
    Network,
    NetworkAttachment,
} from "@/types/network";

jest.mock("@/lib/api/networksApi");

const mockedNetworksApi = jest.mocked(networksApi);

const network: Network = {
    id: "network-1",
    name: "Network 1",
    description: "Test network",
    isActive: true,
    maxAttachments: 4,
    createdAt: "2026-10-03T10:00:00Z",
};

const secondNetwork: Network = {
    id: "network-2",
    name: "Network 2",
    description: null,
    isActive: true,
    maxAttachments: 4,
    createdAt: "2026-10-03T11:00:00Z",
};

const attachment: NetworkAttachment = {
    id: "attachment-1",
    computeNodeId: "node-1",
    networkId: "network-1",
    attachedAt: "2026-10-03T10:00:00Z",
};

function createTestStore() {
    return configureStore({
        reducer: {
            networks: networksReducer,
        },
    });
}

describe("networksSlice", () => {
    beforeEach(() => {
        jest.resetAllMocks();
    });

    describe("fetchNetworks", () => {
        it("loads networks successfully", async () => {
            mockedNetworksApi.getAll.mockResolvedValue([
                network,
            ]);

            const store = createTestStore();

            await store.dispatch(fetchNetworks());

            const state = store.getState().networks;

            expect(state.loading).toBe(false);
            expect(state.error).toBeNull();
            expect(state.networks).toEqual([network]);
        });

        it("stores an error when loading networks fails", async () => {
            mockedNetworksApi.getAll.mockRejectedValue(
                new Error("Network request failed"),
            );

            const store = createTestStore();

            await store.dispatch(fetchNetworks());

            const state = store.getState().networks;

            expect(state.loading).toBe(false);
            expect(state.error).toBe(
                "Network request failed",
            );
        });
    });

    describe("createNetwork", () => {
        it("creates and adds a network", async () => {
            mockedNetworksApi.create.mockResolvedValue(
                network,
            );

            const store = createTestStore();
            store.dispatch({
                type: "networks/fetchNetworks/fulfilled",
                payload: [secondNetwork],
            });

            await store.dispatch(
                createNetwork({
                    name: "Network 1",
                    description: "Test network",
                }),
            );

            const state = store.getState().networks;

            expect(state.creating).toBe(false);
            expect(state.networks).toEqual([
                network,
                secondNetwork,
            ]);
            expect(state.createSuccess).toBe(
                'Network "Network 1" created successfully.',
            );
            expect(state.error).toBeNull();
        });

        it("stores an error when creating a network fails", async () => {
            mockedNetworksApi.create.mockRejectedValue(
                new Error("Create failed"),
            );

            const store = createTestStore();

            await store.dispatch(
                createNetwork({
                    name: "Network 1",
                }),
            );

            const state = store.getState().networks;

            expect(state.creating).toBe(false);
            expect(state.error).toBe("Create failed");
            expect(state.networks).toEqual([]);
        });
    });

    describe("activateNetwork", () => {
        it("activates the requested network", async () => {
            mockedNetworksApi.activate.mockResolvedValue(
                undefined,
            );

            const inactiveNetwork = {
                ...network,
                isActive: false,
            };

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [inactiveNetwork],
                        attachmentsByNetworkId: {},
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                activateNetwork("network-1"),
            );

            expect(
                store.getState().networks.networks[0]
                    .isActive,
            ).toBe(true);
        });

        it("stores an error when activation fails", async () => {
            mockedNetworksApi.activate.mockRejectedValue(
                new Error("Activation failed"),
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network],
                        attachmentsByNetworkId: {},
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                activateNetwork("network-1"),
            );

            expect(
                store.getState().networks.error,
            ).toBe("Activation failed");
        });
    });

    describe("deactivateNetwork", () => {
        it("deactivates the requested network", async () => {
            mockedNetworksApi.deactivate.mockResolvedValue(
                undefined,
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network],
                        attachmentsByNetworkId: {},
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                deactivateNetwork("network-1"),
            );

            expect(
                store.getState().networks.networks[0]
                    .isActive,
            ).toBe(false);
        });

        it("stores an error when deactivation fails", async () => {
            mockedNetworksApi.deactivate.mockRejectedValue(
                new Error("Deactivation failed"),
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network],
                        attachmentsByNetworkId: {},
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                deactivateNetwork("network-1"),
            );

            expect(
                store.getState().networks.error,
            ).toBe("Deactivation failed");
        });
    });

    describe("deleteNetwork", () => {
        it("sets the deleting network id while deletion is in progress", async () => {
            let resolveDelete:
                | (() => void)
                | undefined;

            mockedNetworksApi.delete.mockReturnValue(
                new Promise<void>((resolve) => {
                    resolveDelete = resolve;
                }),
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network, secondNetwork],
                        attachmentsByNetworkId: {},
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            const deletePromise = store.dispatch(
                deleteNetwork("network-1"),
            );

            const pendingState =
                store.getState().networks;

            expect(
                pendingState.deletingNetworkId,
            ).toBe("network-1");

            expect(
                pendingState.deleteErrorByNetworkId[
                    "network-1"
                    ],
            ).toBeNull();

            expect(
                pendingState.networks,
            ).toEqual([
                network,
                secondNetwork,
            ]);

            resolveDelete!();

            await deletePromise;

            const completedState =
                store.getState().networks;

            expect(
                completedState.deletingNetworkId,
            ).toBeNull();

            expect(
                completedState.networks,
            ).toEqual([
                secondNetwork,
            ]);
        });

        it("removes the deleted network and its attachments", async () => {
            mockedNetworksApi.delete.mockResolvedValue(
                undefined,
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network, secondNetwork],
                        attachmentsByNetworkId: {
                            "network-1": {
                                items: [attachment],
                                loading: false,
                                error: null,
                                attachLoading: false,
                                attachSuccess: null,
                                attachError: null,
                            },
                        },
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {
                            "network-1":
                                "Previous delete error",
                        },
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                deleteNetwork("network-1"),
            );

            const state = store.getState().networks;

            expect(
                mockedNetworksApi.delete,
            ).toHaveBeenCalledWith("network-1");

            expect(state.networks).toEqual([
                secondNetwork,
            ]);

            expect(
                state.attachmentsByNetworkId["network-1"],
            ).toBeUndefined();

            expect(
                state.deleteErrorByNetworkId[
                    "network-1"
                    ],
            ).toBeUndefined();

            expect(state.deletingNetworkId).toBeNull();
            expect(state.error).toBeNull();
        });

        it("stores a delete error for the affected network only", async () => {
            mockedNetworksApi.delete.mockRejectedValue(
                new Error(
                    "The request conflicts with the current state of the resource.",
                ),
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network, secondNetwork],
                        attachmentsByNetworkId: {},
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                deleteNetwork("network-1"),
            );

            const state = store.getState().networks;

            expect(
                state.deleteErrorByNetworkId[
                    "network-1"
                    ],
            ).toBe(
                "The network cannot be deleted while nodes are attached.",
            );

            expect(
                state.deleteErrorByNetworkId[
                    "network-2"
                    ],
            ).toBeUndefined();

            expect(state.deletingNetworkId).toBeNull();
            expect(state.networks).toEqual([
                network,
                secondNetwork,
            ]);
        });

        it("clears the previous delete error when deletion starts again", async () => {
            mockedNetworksApi.delete.mockReturnValue(
                new Promise<void>(() => {}),
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network],
                        attachmentsByNetworkId: {},
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {
                            "network-1":
                                "The network cannot be deleted while nodes are attached.",
                        },
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            store.dispatch(
                deleteNetwork("network-1"),
            );

            const state = store.getState().networks;

            expect(
                state.deleteErrorByNetworkId[
                    "network-1"
                    ],
            ).toBeNull();

            expect(
                state.deletingNetworkId,
            ).toBe("network-1");
        });

        it("keeps delete errors isolated between networks", async () => {
            mockedNetworksApi.delete
                .mockRejectedValueOnce(
                    new Error(
                        "The request conflicts with the current state of the resource.",
                    ),
                )
                .mockReturnValueOnce(
                    new Promise<void>(() => {}),
                );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network, secondNetwork],
                        attachmentsByNetworkId: {},
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                deleteNetwork("network-1"),
            );

            store.dispatch(
                deleteNetwork("network-2"),
            );

            const state = store.getState().networks;

            expect(
                state.deleteErrorByNetworkId[
                    "network-1"
                    ],
            ).toBe(
                "The network cannot be deleted while nodes are attached.",
            );

            expect(
                state.deleteErrorByNetworkId[
                    "network-2"
                    ],
            ).toBeNull();

            expect(
                state.deletingNetworkId,
            ).toBe("network-2");
        });
    });

    describe("fetchNetworkAttachments", () => {
        it("loads attachments for the requested network", async () => {
            mockedNetworksApi.getNetworkNodes.mockResolvedValue(
                [attachment],
            );

            const store = createTestStore();

            await store.dispatch(
                fetchNetworkAttachments("network-1"),
            );

            const state =
                store.getState().networks
                    .attachmentsByNetworkId["network-1"];

            expect(state.loading).toBe(false);
            expect(state.error).toBeNull();
            expect(state.items).toEqual([attachment]);
        });

        it("keeps attachment state isolated per network", async () => {
            mockedNetworksApi.getNetworkNodes
                .mockResolvedValueOnce([attachment])
                .mockResolvedValueOnce([]);

            const store = createTestStore();

            await store.dispatch(
                fetchNetworkAttachments("network-1"),
            );

            await store.dispatch(
                fetchNetworkAttachments("network-2"),
            );

            const state =
                store.getState().networks
                    .attachmentsByNetworkId;

            expect(state["network-1"].items).toEqual([
                attachment,
            ]);

            expect(state["network-2"].items).toEqual([]);
        });

        it("stores an attachment loading error", async () => {
            mockedNetworksApi.getNetworkNodes.mockRejectedValue(
                new Error("Attachment loading failed"),
            );

            const store = createTestStore();

            await store.dispatch(
                fetchNetworkAttachments("network-1"),
            );

            const state =
                store.getState().networks
                    .attachmentsByNetworkId["network-1"];

            expect(state.loading).toBe(false);
            expect(state.error).toBe(
                "Attachment loading failed",
            );
        });
    });

    describe("attachNodeToNetwork", () => {
        it("adds an attachment to the correct network", async () => {
            mockedNetworksApi.attach.mockResolvedValue(
                attachment,
            );

            const store = createTestStore();

            await store.dispatch(
                attachNodeToNetwork({
                    nodeId: "node-1",
                    networkId: "network-1",
                }),
            );

            const state =
                store.getState().networks
                    .attachmentsByNetworkId["network-1"];

            expect(state.items).toEqual([attachment]);
            expect(state.attachLoading).toBe(false);
            expect(state.attachSuccess).toBe(
                "Node attached successfully.",
            );
            expect(state.attachError).toBeNull();
        });

        it("stores an attachment error", async () => {
            mockedNetworksApi.attach.mockRejectedValue(
                new Error("Attach failed"),
            );

            const store = createTestStore();

            await store.dispatch(
                attachNodeToNetwork({
                    nodeId: "node-1",
                    networkId: "network-1",
                }),
            );

            const state =
                store.getState().networks
                    .attachmentsByNetworkId["network-1"];

            expect(state.attachLoading).toBe(false);
            expect(state.attachSuccess).toBeNull();
            expect(state.attachError).toBe(
                "Attach failed",
            );
        });
    });

    describe("detachNodeFromNetwork", () => {
        it("removes the attachment from the correct network", async () => {
            mockedNetworksApi.detach.mockResolvedValue(
                undefined,
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network, secondNetwork],
                        attachmentsByNetworkId: {
                            "network-1": {
                                items: [attachment],
                                loading: false,
                                error: null,
                                attachLoading: false,
                                attachSuccess: null,
                                attachError: null,
                            },
                            "network-2": {
                                items: [],
                                loading: false,
                                error: null,
                                attachLoading: false,
                                attachSuccess: null,
                                attachError: null,
                            },
                        },
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                detachNodeFromNetwork({
                    nodeId: "node-1",
                    networkId: "network-1",
                }),
            );

            expect(
                store.getState().networks
                    .attachmentsByNetworkId["network-1"]
                    .items,
            ).toEqual([]);
        });

        it("keeps the attachment when detach fails", async () => {
            mockedNetworksApi.detach.mockRejectedValue(
                new Error("Detach failed"),
            );

            const store = configureStore({
                reducer: {
                    networks: networksReducer,
                },
                preloadedState: {
                    networks: {
                        networks: [network],
                        attachmentsByNetworkId: {
                            "network-1": {
                                items: [attachment],
                                loading: false,
                                error: null,
                                attachLoading: false,
                                attachSuccess: null,
                                attachError: null,
                            },
                        },
                        loading: false,
                        creating: false,
                        deletingNetworkId: null,
                        deleteErrorByNetworkId: {},
                        error: null,
                        createSuccess: null,
                    },
                },
            });

            await store.dispatch(
                detachNodeFromNetwork({
                    nodeId: "node-1",
                    networkId: "network-1",
                }),
            );

            const state =
                store.getState().networks
                    .attachmentsByNetworkId["network-1"];

            expect(state.items).toEqual([attachment]);
            expect(state.attachError).toBe(
                "Detach failed",
            );
        });
    });
});