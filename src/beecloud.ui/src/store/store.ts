import { configureStore } from "@reduxjs/toolkit";
import nodesReducer from "./slices/nodesSlice";
import networksReducer from "./slices/networksSlice";
import { createAsyncThunk, createSelector, createSlice } from "@reduxjs/toolkit";
import type { RootState } from "@/store/store";
export const store = configureStore({
    reducer: {
        nodes: nodesReducer,
        networks: networksReducer,
    },
});
export const selectAttachmentsByNetworkId = createSelector(
    [
        (state: RootState) => state.networks.attachments,
        (_state: RootState, networkId: string) => networkId,
    ],
    (attachments, networkId) =>
        attachments.filter(
            (attachment) =>
                attachment.networkId === networkId,
        ),
);
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;