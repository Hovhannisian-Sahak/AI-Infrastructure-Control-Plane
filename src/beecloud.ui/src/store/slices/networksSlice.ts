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
type NetworksState = {
    networks: Network[];
    attachments: NetworkAttachment[];
    loading: boolean;
    attachmentsLoading: boolean;
    creating: boolean;
    error: string | null;
    attachmentsError: string | null;
    createSuccess: string | null;
};

const initialState: NetworksState = {
    networks: [],
    attachments: [],
    loading: false,
    attachmentsLoading: false,
    creating: false,
    error: null,
    attachmentsError: null,
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
    async (networkId: string, { rejectWithValue }) => {
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
    async (networkId: string, { rejectWithValue }) => {
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

export const fetchNetworkAttachments = createAsyncThunk(
    "networks/fetchNetworkAttachments",
    async (networkId: string, { rejectWithValue }) => {
        try {
            return await networksApi.getNetworkNodes(networkId);
        } catch (error) {
            return rejectWithValue(
                error instanceof Error
                    ? error.message
                    : "Failed to load network attachments.",
            );
        }
    },
);

export const attachNodeToNetwork = createAsyncThunk(
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

export const detachNodeFromNetwork = createAsyncThunk(
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
            .addCase(fetchNetworks.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(fetchNetworks.fulfilled, (state, action) => {
                state.loading = false;
                state.networks = action.payload;
            })
            .addCase(fetchNetworks.rejected, (state, action) => {
                state.loading = false;
                state.error =
                    (action.payload as string) ??
                    "Failed to load networks.";
            })

            .addCase(createNetwork.pending, (state) => {
                state.creating = true;
                state.createSuccess = null;
                state.error = null;
            })
            .addCase(createNetwork.fulfilled, (state, action) => {
                state.creating = false;
                state.networks.push(action.payload);
                state.createSuccess =
                    `Network "${action.payload.name}" created successfully.`;
            })
            .addCase(createNetwork.rejected, (state, action) => {
                state.creating = false;
                state.error =
                    (action.payload as string) ??
                    "Failed to create network.";
            })

            .addCase(activateNetwork.fulfilled, (state, action) => {
                const network = state.networks.find(
                    (item) => item.id === action.payload,
                );

                if (network) {
                    network.isActive = true;
                }
            })
            .addCase(activateNetwork.rejected, (state, action) => {
                state.error =
                    (action.payload as string) ??
                    "Failed to activate network.";
            })

            .addCase(deactivateNetwork.fulfilled, (state, action) => {
                const network = state.networks.find(
                    (item) => item.id === action.payload,
                );

                if (network) {
                    network.isActive = false;
                }
            })
            .addCase(deactivateNetwork.rejected, (state, action) => {
                state.error =
                    (action.payload as string) ??
                    "Failed to deactivate network.";
            })

            .addCase(
                fetchNetworkAttachments.pending,
                (state) => {
                    state.attachmentsLoading = true;
                    state.attachmentsError = null;
                },
            )
            .addCase(
                fetchNetworkAttachments.fulfilled,
                (state, action) => {
                    state.attachmentsLoading = false;
                    state.attachments = action.payload;
                },
            )
            .addCase(
                fetchNetworkAttachments.rejected,
                (state, action) => {
                    state.attachmentsLoading = false;
                    state.attachmentsError =
                        (action.payload as string) ??
                        "Failed to load network attachments.";
                },
            )

            .addCase(
                attachNodeToNetwork.fulfilled,
                (state, action) => {
                    state.attachments.push(action.payload);
                },
            )
            .addCase(
                attachNodeToNetwork.rejected,
                (state, action) => {
                    state.error =
                        (action.payload as string) ??
                        "Failed to attach node to network.";
                },
            )

            .addCase(
                detachNodeFromNetwork.fulfilled,
                (state, action) => {
                    state.attachments =
                        state.attachments.filter(
                            (attachment) =>
                                !(
                                    attachment.computeNodeId ===
                                    action.payload.nodeId &&
                                    attachment.networkId ===
                                    action.payload.networkId
                                ),
                        );
                },
            )
            .addCase(
                detachNodeFromNetwork.rejected,
                (state, action) => {
                    state.error =
                        (action.payload as string) ??
                        "Failed to detach node from network.";
                },
            );
    },
});

export const {
    clearNetworkError,
    clearCreateSuccess,
} = networksSlice.actions;

export default networksSlice.reducer;