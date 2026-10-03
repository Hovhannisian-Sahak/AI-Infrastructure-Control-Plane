import { createSelector } from "@reduxjs/toolkit";
import type { RootState } from "@/store/store";

export const selectAttachmentsByNetworkId =
    createSelector(
        [
            (state: RootState) =>
                state.networks.attachments,
            (_state: RootState, networkId: string) =>
                networkId,
        ],
        (attachments, networkId) =>
            attachments.filter(
                (attachment) =>
                    attachment.networkId === networkId,
            ),
    );