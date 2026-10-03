import { createSelector } from "@reduxjs/toolkit";
import type { RootState } from "@/store/store";

export const selectAttachmentsByNetworkId =
    createSelector(
        [
            (state: RootState) =>
                state.networks
                    .attachmentsByNetworkId,
            (_state: RootState, networkId: string) =>
                networkId,
        ],
        (
            attachmentsByNetworkId,
            networkId,
        ) =>
            attachmentsByNetworkId[networkId]
                ?.items ?? [],
    );