"use client";

import { useEffect, useState } from "react";
import { Network } from "@/types/network";
import {
  useAppDispatch,
  useAppSelector,
} from "@/store/hooks";
import {
  activateNetwork,
  attachNodeToNetwork,
  deactivateNetwork,
  deleteNetwork,
  detachNodeFromNetwork,
  clearAttachSuccess,
  fetchNetworkAttachments,
} from "@/store/slices/networksSlice";
import styles from "./NetworkCard.module.css";
import Pagination from "@/components/common/Pagination";

const ATTACHMENT_PAGE_SIZE = 3;

type NetworkCardProps = {
  network: Network;
};

export default function NetworkCard({
                                      network,
                                    }: NetworkCardProps) {
  const dispatch = useAppDispatch();

  const [showAttachments, setShowAttachments] =
      useState(false);

  const [selectedNodeId, setSelectedNodeId] =
      useState("");
  const [attachmentPage, setAttachmentPage] = useState(1);

  const nodes = useAppSelector(
      (state) => state.nodes.nodes,
  );

  const attachmentState = useAppSelector(
      (state) =>
          state.networks.attachmentsByNetworkId[
              network.id
              ],
  );

  const deletingNetworkId = useAppSelector(
      (state) => state.networks.deletingNetworkId,
  );

  const deleting =
      deletingNetworkId === network.id;

  const networkError = useAppSelector(
      (state) =>
          state.networks.deleteErrorByNetworkId[
              network.id
              ] ?? null,
  );

  const attachments =
      attachmentState?.items ?? [];

  const attachmentsLoading =
      attachmentState?.loading ?? false;

  const attachmentsError =
      attachmentState?.error ?? null;

  const attachLoading =
      attachmentState?.attachLoading ?? false;

  const attachSuccess =
      attachmentState?.attachSuccess ?? null;

  const attachError =
      attachmentState?.attachError ?? null;

  const availableNodes = nodes.filter(
      (node) =>
          !attachments.some(
              (attachment) =>
                  attachment.computeNodeId === node.id,
          ),
  );

  const maximumReached =
      attachments.length >=
      network.maxAttachments;
  const attachmentPageCount =
      Math.ceil(attachments.length / ATTACHMENT_PAGE_SIZE);
  const currentAttachmentPage = Math.min(
      attachmentPage,
      Math.max(1, attachmentPageCount),
  );
  const visibleAttachments = attachments.slice(
      (currentAttachmentPage - 1) * ATTACHMENT_PAGE_SIZE,
      currentAttachmentPage * ATTACHMENT_PAGE_SIZE,
  );

  useEffect(() => {
    dispatch(
        fetchNetworkAttachments(network.id),
    );
  }, [dispatch, network.id]);

  useEffect(() => {
    if (!attachSuccess) {
      return;
    }

    const timeoutId = setTimeout(() => {
      dispatch(clearAttachSuccess(network.id));
    }, 3000);

    return () => clearTimeout(timeoutId);
  }, [attachSuccess, dispatch, network.id]);

  const handleToggleAttachments = () => {
    setShowAttachments(
        (current) => !current,
    );
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

    if (
        attachNodeToNetwork.fulfilled.match(
            result,
        )
    ) {
      setSelectedNodeId("");
    }
  };

  const handleDetachNode = (
      nodeId: string,
  ) => {
    dispatch(
        detachNodeFromNetwork({
          nodeId,
          networkId: network.id,
        }),
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
              !network.isActive
                  ? styles.inactive
                  : ""
          }`}
      >
        <div className={styles.header}>
          <div className={styles.heading}>
            <div className={styles.titleRow}>
            <span
                className={styles.networkIcon}
                aria-hidden="true"
            >
              N
            </span>

              <h2 className={styles.name}>
                {network.name}
              </h2>
            </div>

            {network.description && (
                <p className={styles.description}>
                  {network.description}
                </p>
            )}
          </div>

          <span
              className={`${styles.status} ${
                  network.isActive
                      ? styles.active
                      : styles.inactiveStatus
              }`}
          >
          <span
              className={styles.statusDot}
              aria-hidden="true"
          />

            {network.isActive
                ? "Active"
                : "Inactive"}
        </span>
        </div>

        <div className={styles.meta}>
          <div className={styles.metaItem}>
          <span className={styles.metaLabel}>
            Attachments
          </span>

            <strong className={styles.metaValue}>
              {attachments.length} / {network.maxAttachments}
            </strong>
          </div>

          <div className={styles.metaDivider} />

          <div className={styles.metaItem}>
          <span className={styles.metaLabel}>
            Created
          </span>

            <strong className={styles.metaDate}>
              {new Date(
                  network.createdAt,
              ).toLocaleDateString()}
            </strong>
          </div>
        </div>

        <div className={styles.actions}>
          <button
              type="button"
              className={styles.secondaryButton}
              onClick={
                handleToggleAttachments
              }
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
            {network.isActive
                ? "Deactivate"
                : "Activate"}
          </button>

          <button
              type="button"
              className={styles.deleteButton}
              onClick={handleDelete}
              disabled={deleting}
          >
            {deleting
                ? "Deleting..."
                : "Delete"}
          </button>
        </div>

        {networkError && (
            <p
                className={styles.error}
                role="alert"
            >
              {networkError}
            </p>
        )}

        {showAttachments && (
            <div
                className={styles.attachmentsSection}
            >
              <div className={styles.subsectionHeader}>
                <div>
                  <h3>Attached Nodes</h3>

                  <p>
                    Manage nodes connected to this
                    network.
                  </p>
                </div>

                <span className={styles.subsectionCount}>
              {attachments.length}
            </span>
              </div>

              {attachmentsLoading && (
                  <div className={styles.loading}>
              <span
                  className={styles.spinner}
                  aria-hidden="true"
              />
                    Loading attachments...
                  </div>
              )}

              {attachmentsError && (
                  <p
                      className={styles.error}
                      role="alert"
                  >
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

              {attachments.length > 0 && (
                      <ul
                          className={
                            styles.attachmentList
                          }
                      >
                        {visibleAttachments.map(
                            (attachment) => (
                                <li
                                    key={attachment.id}
                                    className={
                                      styles.attachmentItem
                                    }
                                >
                                  <div
                                      className={
                                        styles.attachmentInfo
                                      }
                                  >
                        <span
                            className={
                              styles.nodeIndicator
                            }
                            aria-hidden="true"
                        />

                                    <div>
                          <span
                              className={
                                styles.attachmentLabel
                              }
                          >
                            Node
                          </span>

                                      <span
                                          className={
                                            styles.attachmentName
                                          }
                                      >
                            {nodes.find(
                                    (node) =>
                                        node.id ===
                                        attachment.computeNodeId,
                                )?.name ??
                                attachment.computeNodeId}
                          </span>
                                    </div>
                                  </div>

                                  <button
                                      type="button"
                                      className={
                                        styles.smallDangerButton
                                      }
                                      onClick={() =>
                                          handleDetachNode(
                                              attachment.computeNodeId,
                                          )
                                      }
                                  >
                                    Detach
                                  </button>
                                </li>
                            ),
                        )}
                      </ul>
                  )}
              {attachments.length > 0 && (
                      <Pagination
                          page={currentAttachmentPage}
                          pageSize={ATTACHMENT_PAGE_SIZE}
                          totalItems={attachments.length}
                          ariaLabel={`Attachments for ${network.name}`}
                          onPageChange={setAttachmentPage}
                      />
                  )}
            </div>
        )}

        <div className={styles.attachSection}>
          <div className={styles.subsectionHeader}>
            <div>
              <h3>Attach Node</h3>

              <p>
                Connect an available compute node
                to this network.
              </p>
            </div>
          </div>

          {!network.isActive && (
              <p className={styles.warning}>
                Activate this network before
                attaching nodes.
              </p>
          )}

          {network.isActive &&
              maximumReached && (
                  <p className={styles.warning}>
                    Maximum attachment limit reached (
                    {network.maxAttachments}).
                  </p>
              )}

          {network.isActive &&
              !maximumReached &&
              availableNodes.length === 0 && (
                  <p className={styles.muted}>
                    All nodes are already attached to
                    this network.
                  </p>
              )}

          {network.isActive &&
              !maximumReached &&
              availableNodes.length > 0 && (
                  <div
                      className={
                        styles.attachControls
                      }
                  >
                    <select
                        className={styles.select}
                        value={selectedNodeId}
                        onChange={(event) =>
                            setSelectedNodeId(
                                event.target.value,
                            )
                        }
                        disabled={attachLoading}
                    >
                      <option value="">
                        Select a node
                      </option>

                      {availableNodes.map(
                          (node) => (
                              <option
                                  key={node.id}
                                  value={node.id}
                              >
                                {node.name}
                              </option>
                          ),
                      )}
                    </select>

                    <button
                        type="button"
                        className={
                          styles.primaryButton
                        }
                        disabled={
                            !selectedNodeId ||
                            attachLoading
                        }
                        onClick={handleAttachNode}
                    >
                      {attachLoading
                          ? "Attaching..."
                          : "Attach Node"}
                    </button>
                  </div>
              )}

          {attachError && (
              <p
                  className={styles.error}
                  role="alert"
              >
                {attachError}
              </p>
          )}
        </div>

        {attachSuccess && (
            <div
                className={styles.successAlert}
                role="status"
            >
          <span
              className={styles.successIcon}
              aria-hidden="true"
          >
            ✓
          </span>

              Node attached successfully.
            </div>
        )}
      </article>
  );
}