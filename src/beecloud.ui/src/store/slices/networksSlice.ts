import {
    createAsyncThunk,
    createSlice,
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
                    : "Failed to load networks.",
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
                    : "Failed to create network.",
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
                    : "Failed to activate network.",
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
                    : "Failed to deactivate network.",
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
                        : "Failed to load network attachments.",
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
                        : "Failed to attach node to network.",
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
                        : "Failed to detach node from network.",
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
                    state.networks =
                        action.payload;
                },
            )

            .addCase(
                fetchNetworks.rejected,
                (state, action) => {
                    state.loading = false;
                    state.error =
                        (action.payload as string) ??
                        "Failed to load networks.";
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

                    state.networks.push(
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

                    state.error =
                        (action.payload as string) ??
                        "Failed to create network.";
                },
            )

            // -----------------------------
            // Activate network
            // -----------------------------

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
                        "Failed to activate network.";
                },
            )

            // -----------------------------
            // Deactivate network
            // -----------------------------

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
                        "Failed to deactivate network.";
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
                            "Failed to load network attachments.",
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
                            "Failed to attach node to network.",
                    };
                },
            )

            // -----------------------------
            // Detach node
            // -----------------------------

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
                            "Failed to detach node from network.",
                    };
                },
            );
    },
});

export const {
    clearNetworkError,
    clearCreateSuccess,
} = networksSlice.actions;

export default networksSlice.reducer;