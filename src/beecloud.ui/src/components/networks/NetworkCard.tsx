"use client";

import { useEffect, useState } from "react";
import { Network } from "@/types/network";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
  activateNetwork,
  attachNodeToNetwork,
  deactivateNetwork,
  deleteNetwork,
  detachNodeFromNetwork,
  fetchNetworkAttachments,
} from "@/store/slices/networksSlice";
import styles from "./NetworkCard.module.css";

type NetworkCardProps = {
  network: Network;
};

export default function NetworkCard({ network }: NetworkCardProps) {
  const dispatch = useAppDispatch();

  const [showAttachments, setShowAttachments] = useState(false);

  const [selectedNodeId, setSelectedNodeId] = useState("");

  const [showAttachSuccess, setShowAttachSuccess] = useState(false);

  const nodes = useAppSelector((state) => state.nodes.nodes);

  const attachmentState = useAppSelector(
      (state) => state.networks.attachmentsByNetworkId[network.id],
  );

  const deletingNetworkId = useAppSelector(
      (state) => state.networks.deletingNetworkId,
  );

  const deleting = deletingNetworkId === network.id;
  
  const networkError = useAppSelector(
      (state) =>
          state.networks.deleteErrorByNetworkId[
              network.id
              ] ?? null,
  );

  const attachments = attachmentState?.items ?? [];

  const attachmentsLoading = attachmentState?.loading ?? false;

  const attachmentsError = attachmentState?.error ?? null;

  const attachLoading = attachmentState?.attachLoading ?? false;

  const attachSuccess = attachmentState?.attachSuccess ?? null;

  const attachError = attachmentState?.attachError ?? null;

  const availableNodes = nodes.filter(
      (node) =>
          !attachments.some(
              (attachment) => attachment.computeNodeId === node.id,
          ),
  );

  const maximumReached = attachments.length >= network.maxAttachments;

  useEffect(() => {
    dispatch(fetchNetworkAttachments(network.id));
  }, [dispatch, network.id]);

  useEffect(() => {
    if (!attachSuccess) {
      return;
    }

    setShowAttachSuccess(true);

    const timeoutId = setTimeout(() => {
      setShowAttachSuccess(false);
    }, 3000);

    return () => clearTimeout(timeoutId);
  }, [attachSuccess]);

  const handleToggleAttachments = () => {
    setShowAttachments((current) => !current);
  };

  const handleAttachNode = async () => {
    if (!selectedNodeId) {
      return;
    }

    const result = await dispatch(
        attachNodeToNetwork({
          nodeId: selectedNodeId,
          networkId: network.id,
        }),
    );

    if (attachNodeToNetwork.fulfilled.match(result)) {
      setSelectedNodeId("");
    }
  };

  const handleDetachNode = (nodeId: string) => {
    dispatch(
        detachNodeFromNetwork({
          nodeId,
          networkId: network.id,
        }),
    );
  };

  const handleToggleActive = () => {
    if (network.isActive) {
      dispatch(deactivateNetwork(network.id));
    } else {
      dispatch(activateNetwork(network.id));
    }
  };

  const handleDelete = () => {
    const confirmed = window.confirm(
        `Are you sure you want to delete "${network.name}"?`,
    );

    if (!confirmed) {
      return;
    }

    dispatch(deleteNetwork(network.id));
  };

  return (
      <article
          className={`${styles.card} ${
              !network.isActive ? styles.inactive : ""
          }`}
      >
        <div className={styles.header}>
          <div>
            <h2 className={styles.name}>{network.name}</h2>

            {network.description && (
                <p className={styles.description}>{network.description}</p>
            )}
          </div>

          <span
              className={`${styles.status} ${
                  network.isActive
                      ? styles.active
                      : styles.inactiveStatus
              }`}
          >
          {network.isActive ? "Active" : "Inactive"}
        </span>
        </div>

        <div className={styles.meta}>
          <span>Attachments:</span>
          <strong>
            {" "}
            {attachments.length} / {network.maxAttachments}
          </strong>

          <span>
          Created {new Date(network.createdAt).toLocaleDateString()}
        </span>
        </div>

        <div className={styles.actions}>
          <button
              type="button"
              className={styles.secondaryButton}
              onClick={handleToggleAttachments}
              disabled={deleting}
          >
            {showAttachments
                ? "Hide Attachments"
                : "Show Attachments"}
          </button>

          <button
              type="button"
              className={
                network.isActive
                    ? styles.dangerButton
                    : styles.primaryButton
              }
              onClick={handleToggleActive}
              disabled={deleting}
          >
            {network.isActive ? "Deactivate" : "Activate"}
          </button>

          <button
              type="button"
              className={styles.dangerButton}
              onClick={handleDelete}
              disabled={deleting}
          >
            {deleting ? "Deleting..." : "Delete"}
          </button>
        </div>
        {networkError && (
            <p className={styles.error} role="alert">
              {networkError}
            </p>
        )}
        {showAttachments && (
            <div className={styles.attachmentsSection}>
              <h3>Attached Nodes</h3>

              {attachmentsLoading && (
                  <p className={styles.muted}>
                    Loading attachments...
                  </p>
              )}

              {attachmentsError && (
                  <p className={styles.error} role="alert">
                    {attachmentsError}
                  </p>
              )}

              {!attachmentsLoading &&
                  !attachmentsError &&
                  attachments.length === 0 && (
                      <p className={styles.muted}>
                        No nodes attached.
                      </p>
                  )}

              {!attachmentsLoading &&
                  !attachmentsError &&
                  attachments.length > 0 && (
                      <ul className={styles.attachmentList}>
                        {attachments.map((attachment) => (
                            <li
                                key={attachment.id}
                                className={styles.attachmentItem}
                            >
                              <>
                                <span>Node:</span>
                                <span>
                        {nodes.find(
                                (node) =>
                                    node.id ===
                                    attachment.computeNodeId,
                            )?.name ??
                            attachment.computeNodeId}
                      </span>
                              </>

                              <button
                                  type="button"
                                  className={styles.smallDangerButton}
                                  onClick={() =>
                                      handleDetachNode(
                                          attachment.computeNodeId,
                                      )
                                  }
                              >
                                Detach
                              </button>
                            </li>
                        ))}
                      </ul>
                  )}
            </div>
        )}

        <div className={styles.attachSection}>
          <h3>Attach Node</h3>

          {!network.isActive && (
              <p className={styles.warning}>
                Activate this network before attaching nodes.
              </p>
          )}

          {network.isActive && maximumReached && (
              <p className={styles.warning}>
                Maximum attachment limit reached (
                {network.maxAttachments}).
              </p>
          )}

          {network.isActive &&
              !maximumReached &&
              availableNodes.length === 0 && (
                  <p className={styles.muted}>
                    All nodes are already attached to this network.
                  </p>
              )}

          {network.isActive &&
              !maximumReached &&
              availableNodes.length > 0 && (
                  <div className={styles.attachControls}>
                    <select
                        className={styles.select}
                        value={selectedNodeId}
                        onChange={(event) =>
                            setSelectedNodeId(event.target.value)
                        }
                        disabled={attachLoading}
                    >
                      <option value="">Select a node</option>

                      {availableNodes.map((node) => (
                          <option key={node.id} value={node.id}>
                            {node.name}
                          </option>
                      ))}
                    </select>

                    <button
                        type="button"
                        className={styles.primaryButton}
                        disabled={!selectedNodeId || attachLoading}
                        onClick={handleAttachNode}
                    >
                      {attachLoading ? "Attaching..." : "Attach"}
                    </button>
                  </div>
              )}

          {attachError && (
              <p className={styles.error} role="alert">
                {attachError}
              </p>
          )}
        </div>

        {showAttachSuccess && (
            <div className={styles.successAlert} role="status">
              Node attached successfully.
            </div>
        )}
      </article>
  );
}