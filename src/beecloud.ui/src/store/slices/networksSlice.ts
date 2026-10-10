import {
    createAsyncThunk,
    createSlice,
    type PayloadAction,
} from "@reduxjs/toolkit";
import { networksApi } from "@/lib/api/networksApi";
import {
    CreateNetworkRequest,
    Network,
    NetworkAttachment,
} from "@/types/network";

type NetworkAttachmentState = {
    items: NetworkAttachment[];
    loading: boolean;
    error: string | null;
    attachLoading: boolean;
    attachSuccess: string | null;
    attachError: string | null;
};

type NetworksState = {
    networks: Network[];
    attachmentsByNetworkId: Record<
        string,
        NetworkAttachmentState
    >;
    loading: boolean;
    creating: boolean;
    deletingNetworkId: string | null;
    deleteErrorByNetworkId: Record<string, string | null>;
    error: string | null;
    createSuccess: string | null;
};

const createEmptyAttachmentState =
    (): NetworkAttachmentState => ({
        items: [],
        loading: false,
        error: null,
        attachLoading: false,
        attachSuccess: null,
        attachError: null,
    });

const initialState: NetworksState = {
    networks: [],
    attachmentsByNetworkId: {},
    loading: false,
    creating: false,
    deletingNetworkId: null,
    deleteErrorByNetworkId: {},
    error: null,
    createSuccess: null,
};

export const fetchNetworks = createAsyncThunk(
    "networks/fetchNetworks",
    async (_, { rejectWithValue }) => {
        try {
            return await networksApi.getAll();
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Unable to load networks. Please try again.",
            );
        }
    },
);

export const createNetwork = createAsyncThunk(
    "networks/createNetwork",
    async (
        request: CreateNetworkRequest,
        { rejectWithValue },
    ) => {
        try {
            return await networksApi.create(request);
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Unable to create the network. Please try again.",
            );
        }
    },
);

export const activateNetwork = createAsyncThunk(
    "networks/activateNetwork",
    async (
        networkId: string,
        { rejectWithValue },
    ) => {
        try {
            await networksApi.activate(networkId);

            return networkId;
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Unable to activate the network. Please try again.",
            );
        }
    },
);

export const deactivateNetwork = createAsyncThunk(
    "networks/deactivateNetwork",
    async (
        networkId: string,
        { rejectWithValue },
    ) => {
        try {
            await networksApi.deactivate(networkId);

            return networkId;
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Unable to deactivate the network. Please try again.",
            );
        }
    },
);

export const deleteNetwork = createAsyncThunk(
    "networks/deleteNetwork",
    async (
        networkId: string,
        { rejectWithValue },
    ) => {
        try {
            await networksApi.delete(networkId);

            return networkId;
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Unable to delete the network. Please try again.",
            );
        }
    },
);

export const fetchNetworkAttachments =
    createAsyncThunk(
        "networks/fetchNetworkAttachments",
        async (
            networkId: string,
            { rejectWithValue },
        ) => {
            try {
                return await networksApi.getNetworkNodes(
                    networkId,
                );
            } catch (error) {
                return rejectWithValue(
                    error instanceof Error
                        ? error.message
                        : "Unable to load network attachments. Please try again.",
                );
            }
        },
    );

export const attachNodeToNetwork =
    createAsyncThunk(
        "networks/attachNodeToNetwork",
        async (
            {
                nodeId,
                networkId,
            }: {
                nodeId: string;
                networkId: string;
            },
            { rejectWithValue },
        ) => {
            try {
                return await networksApi.attach(
                    nodeId,
                    networkId,
                );
            } catch (error) {
                return rejectWithValue(
                    error instanceof Error
                        ? error.message
                        : "Unable to attach the node to the network. Please try again.",
                );
            }
        },
    );

export const detachNodeFromNetwork =
    createAsyncThunk(
        "networks/detachNodeFromNetwork",
        async (
            {
                nodeId,
                networkId,
            }: {
                nodeId: string;
                networkId: string;
            },
            { rejectWithValue },
        ) => {
            try {
                await networksApi.detach(
                    nodeId,
                    networkId,
                );

                return {
                    nodeId,
                    networkId,
                };
            } catch (error) {
                return rejectWithValue(
                    error instanceof Error
                        ? error.message
                        : "Unable to detach the node from the network. Please try again.",
                );
            }
        },
    );

const networksSlice = createSlice({
    name: "networks",
    initialState,

    reducers: {
        clearNetworkError(state) {
            state.error = null;
        },

        clearCreateSuccess(state) {
            state.createSuccess = null;
        },

        clearAttachSuccess(
            state,
            action: PayloadAction<string>,
        ) {
            const attachmentState =
                state.attachmentsByNetworkId[
                    action.payload
                    ];

            if (attachmentState) {
                attachmentState.attachSuccess = null;
            }
        },
    },

    extraReducers: (builder) => {
        builder

            // -----------------------------
            // Fetch networks
            // -----------------------------

            .addCase(
                fetchNetworks.pending,
                (state) => {
                    state.loading = true;
                    state.error = null;
                },
            )

            .addCase(
                fetchNetworks.fulfilled,
                (state, action) => {
                    state.loading = false;
                    state.networks = action.payload;
                },
            )

            .addCase(
                fetchNetworks.rejected,
                (state, action) => {
                    state.loading = false;
                    state.error =
                        (action.payload as string) ??
                        action.error.message ??
                        "Unable to load networks. Please try again.";
                },
            )

            // -----------------------------
            // Create network
            // -----------------------------

            .addCase(
                createNetwork.pending,
                (state) => {
                    state.creating = true;
                    state.createSuccess = null;
                    state.error = null;
                },
            )

            .addCase(
                createNetwork.fulfilled,
                (state, action) => {
                    state.creating = false;

                    state.networks.unshift(
                        action.payload,
                    );

                    state.createSuccess =
                        `Network "${action.payload.name}" created successfully.`;
                },
            )

            .addCase(
                createNetwork.rejected,
                (state, action) => {
                    state.creating = false;
                    state.createSuccess = null;

                    const error =
                        (action.payload as string) ??
                        action.error.message ??
                        "Unable to create the network. Please try again.";

                    state.error =
                        error.includes(
                            "conflicts with the current state",
                        )
                            ? "A network with this name already exists."
                            : error;
                },
            )

            // -----------------------------
            // Delete network
            // -----------------------------

            .addCase(
                deleteNetwork.pending,
                (state, action) => {
                    const networkId = action.meta.arg;

                    state.deletingNetworkId = networkId;
                    state.deleteErrorByNetworkId[networkId] = null;
                },
            )

            .addCase(
                deleteNetwork.fulfilled,
                (state, action) => {
                    const networkId = action.payload;

                    state.networks =
                        state.networks.filter(
                            (network) =>
                                network.id !== networkId,
                        );

                    delete state.attachmentsByNetworkId[
                        networkId
                        ];

                    delete state.deleteErrorByNetworkId[
                        networkId
                        ];

                    state.deletingNetworkId = null;
                },
            )

            .addCase(
                deleteNetwork.rejected,
                (state, action) => {
                    const networkId = action.meta.arg;

                    const error =
                        (action.payload as string) ??
                        action.error.message ??
                        "Unable to delete the network. Please try again.";

                    state.deletingNetworkId = null;

                    const attachmentConflict =
                        error.toLowerCase().includes(
                            "cannot be deleted while nodes are attached",
                        ) ||
                        error.toLowerCase().includes(
                            "conflicts with the current state",
                        );

                    state.deleteErrorByNetworkId[networkId] =
                        attachmentConflict
                            ? "The network cannot be deleted while nodes are attached."
                            : error;
                },
            )

            // -----------------------------
            // Activate network
            // -----------------------------

            .addCase(
                activateNetwork.pending,
                (state) => {
                    state.error = null;
                },
            )

            .addCase(
                activateNetwork.fulfilled,
                (state, action) => {
                    const network =
                        state.networks.find(
                            (item) =>
                                item.id ===
                                action.payload,
                        );

                    if (network) {
                        network.isActive = true;
                    }
                },
            )

            .addCase(
                activateNetwork.rejected,
                (state, action) => {
                    state.error =
                        (action.payload as string) ??
                        action.error.message ??
                        "Unable to activate the network. Please try again.";
                },
            )

            // -----------------------------
            // Deactivate network
            // -----------------------------

            .addCase(
                deactivateNetwork.pending,
                (state) => {
                    state.error = null;
                },
            )

            .addCase(
                deactivateNetwork.fulfilled,
                (state, action) => {
                    const network =
                        state.networks.find(
                            (item) =>
                                item.id ===
                                action.payload,
                        );

                    if (network) {
                        network.isActive = false;
                    }
                },
            )

            .addCase(
                deactivateNetwork.rejected,
                (state, action) => {
                    state.error =
                        (action.payload as string) ??
                        action.error.message ??
                        "Unable to deactivate the network. Please try again.";
                },
            )

            // -----------------------------
            // Fetch attachments
            // -----------------------------

            .addCase(
                fetchNetworkAttachments.pending,
                (state, action) => {
                    const networkId =
                        action.meta.arg;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ] ??
                        createEmptyAttachmentState();

                    state.attachmentsByNetworkId[
                        networkId
                        ] = {
                        ...current,
                        loading: true,
                        error: null,
                    };
                },
            )

            .addCase(
                fetchNetworkAttachments.fulfilled,
                (state, action) => {
                    const networkId =
                        action.meta.arg;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ] ??
                        createEmptyAttachmentState();

                    state.attachmentsByNetworkId[
                        networkId
                        ] = {
                        ...current,
                        items: action.payload,
                        loading: false,
                        error: null,
                    };
                },
            )

            .addCase(
                fetchNetworkAttachments.rejected,
                (state, action) => {
                    const networkId =
                        action.meta.arg;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ] ??
                        createEmptyAttachmentState();

                    state.attachmentsByNetworkId[
                        networkId
                        ] = {
                        ...current,
                        loading: false,
                        error:
                            (action.payload as string) ??
                            action.error.message ??
                            "Unable to load network attachments. Please try again.",
                    };
                },
            )

            // -----------------------------
            // Attach node
            // -----------------------------

            .addCase(
                attachNodeToNetwork.pending,
                (state, action) => {
                    const { networkId } =
                        action.meta.arg;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ] ??
                        createEmptyAttachmentState();

                    state.attachmentsByNetworkId[
                        networkId
                        ] = {
                        ...current,
                        attachLoading: true,
                        attachSuccess: null,
                        attachError: null,
                    };
                },
            )

            .addCase(
                attachNodeToNetwork.fulfilled,
                (state, action) => {
                    const attachment =
                        action.payload;

                    const networkId =
                        attachment.networkId;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ] ??
                        createEmptyAttachmentState();

                    state.attachmentsByNetworkId[
                        networkId
                        ] = {
                        ...current,
                        items: [
                            ...current.items,
                            attachment,
                        ],
                        attachLoading: false,
                        attachSuccess:
                            "Node attached successfully.",
                        attachError: null,
                    };
                },
            )

            .addCase(
                attachNodeToNetwork.rejected,
                (state, action) => {
                    const { networkId } =
                        action.meta.arg;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ] ??
                        createEmptyAttachmentState();

                    state.attachmentsByNetworkId[
                        networkId
                        ] = {
                        ...current,
                        attachLoading: false,
                        attachSuccess: null,
                        attachError:
                            (action.payload as string) ??
                            action.error.message ??
                            "Unable to attach the node to the network. Please try again.",
                    };
                },
            )

            // -----------------------------
            // Detach node
            // -----------------------------

            .addCase(
                detachNodeFromNetwork.pending,
                (state, action) => {
                    const { networkId } =
                        action.meta.arg;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ] ??
                        createEmptyAttachmentState();

                    state.attachmentsByNetworkId[
                        networkId
                        ] = {
                        ...current,
                        attachError: null,
                    };
                },
            )

            .addCase(
                detachNodeFromNetwork.fulfilled,
                (state, action) => {
                    const {
                        nodeId,
                        networkId,
                    } = action.payload;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ];

                    if (!current) {
                        return;
                    }

                    current.items =
                        current.items.filter(
                            (attachment) =>
                                !(
                                    attachment.computeNodeId ===
                                    nodeId &&
                                    attachment.networkId ===
                                    networkId
                                ),
                        );

                    current.attachError = null;
                },
            )

            .addCase(
                detachNodeFromNetwork.rejected,
                (state, action) => {
                    const { networkId } =
                        action.meta.arg;

                    const current =
                        state.attachmentsByNetworkId[
                            networkId
                            ] ??
                        createEmptyAttachmentState();

                    state.attachmentsByNetworkId[
                        networkId
                        ] = {
                        ...current,
                        attachError:
                            (action.payload as string) ??
                            action.error.message ??
                            "Unable to detach the node from the network. Please try again.",
                    };
                },
            );
    },
});

export const {
    clearNetworkError,
    clearCreateSuccess,
    clearAttachSuccess,
} = networksSlice.actions;

export default networksSlice.reducer;