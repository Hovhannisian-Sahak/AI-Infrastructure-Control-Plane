"use client";

import { useState } from "react";
import { Network } from "@/types/network";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
    activateNetwork,
    deactivateNetwork,
    fetchNetworkAttachments,
} from "@/store/slices/networksSlice";

type NetworkCardProps = {
    network: Network;
};

export default function NetworkCard({
                                        network,
                                    }: NetworkCardProps) {
    const dispatch = useAppDispatch();

    const [showAttachments, setShowAttachments] =
        useState(false);

    const attachments = useAppSelector(
        (state) => state.networks.attachments,
    ).filter(
        (attachment) =>
            attachment.networkId === network.id,
    );

    const attachmentsLoading = useAppSelector(
        (state) =>
            state.networks.attachmentsLoading,
    );

    const attachmentsError = useAppSelector(
        (state) =>
            state.networks.attachmentsError,
    );

    const handleToggleAttachments = async () => {
        if (showAttachments) {
            setShowAttachments(false);
            return;
        }

        setShowAttachments(true);

        await dispatch(
            fetchNetworkAttachments(network.id),
        );
    };

    const handleToggleActive = () => {
        if (network.isActive) {
            dispatch(
                deactivateNetwork(network.id),
            );
        } else {
            dispatch(
                activateNetwork(network.id),
            );
        }
    };

    return (
        <article>
            <div>
                <h2>{network.name}</h2>

                {network.description && (
                    <p>{network.description}</p>
                )}
            </div>

            <div>
                <span>
                    {network.isActive
                        ? "Active"
                        : "Inactive"}
                </span>
            </div>

            <div>
                <p>
                    Attachments:{" "}
                    {attachments.length} /{" "}
                    {network.maxAttachments}
                </p>

                <button
                    type="button"
                    onClick={handleToggleAttachments}
                >
                    {showAttachments
                        ? "Hide Attachments"
                        : "Show Attachments"}
                </button>

                {showAttachments && (
                    <div
                        style={{
                            minHeight: "60px",
                        }}
                    >
                        {attachmentsLoading && (
                            <p>Loading attachments...</p>
                        )}

                        {attachmentsError && (
                            <p role="alert">
                                {attachmentsError}
                            </p>
                        )}

                        {!attachmentsLoading &&
                            !attachmentsError &&
                            attachments.length === 0 && (
                                <p>No nodes attached.</p>
                            )}

                        {!attachmentsLoading &&
                            !attachmentsError &&
                            attachments.length > 0 && (
                                <ul>
                                    {attachments.map((attachment) => (
                                        <li key={attachment.id}>
                                            Node: {attachment.computeNodeId}
                                        </li>
                                    ))}
                                </ul>
                            )}
                    </div>
                )}

                <button
                    type="button"
                    onClick={handleToggleActive}
                >
                    {network.isActive
                        ? "Deactivate"
                        : "Activate"}
                </button>
            </div>
        </article>
    );
}