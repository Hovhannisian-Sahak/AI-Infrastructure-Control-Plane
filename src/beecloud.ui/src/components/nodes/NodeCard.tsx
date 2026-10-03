"use client";

import type {
  ComputeNode,
  NodeStatus,
} from "@/lib/api/models/computeNode";
import {
  useAppDispatch,
  useAppSelector,
} from "@/store/hooks";
import {
  restartNode,
  startNode,
  stopNode,
} from "@/store/slices/nodesSlice";
import styles from "./NodeCard.module.css";

type NodeCardProps = {
  node: ComputeNode;
};

const statusClassMap: Record<NodeStatus, string> = {
  Provisioning: styles.provisioning,
  Available: styles.available,
  Running: styles.running,
  Stopping: styles.stopping,
  Stopped: styles.stopped,
  Unhealthy: styles.unhealthy,
  Quarantined: styles.quarantined,
  Remediating: styles.remediating,
  Failed: styles.failed,
};

export default function NodeCard({
                                   node,
                                 }: NodeCardProps) {
  const dispatch = useAppDispatch();

  const actionLoading = useAppSelector(
      (state) =>
          state.nodes.actionLoadingByNodeId[
              node.id
              ] ?? false,
  );

  const canStart =
      node.status === "Available" ||
      node.status === "Stopped";

  const canStop =
      node.status === "Running";

  const canRestart =
      node.status === "Running";

  const hasActiveFault =
      node.activeFault !== "None";

  return (
      <article className={styles.card}>
        <div className={styles.header}>
          <div className={styles.heading}>
            <h2 className={styles.title}>
              {node.name}
            </h2>

            <p className={styles.nodeId}>
              {node.id}
            </p>
          </div>

          <span
              className={`${styles.status} ${
                  statusClassMap[node.status]
              }`}
          >
          {node.status}
        </span>
        </div>

        <dl className={styles.details}>
          <div className={styles.detail}>
            <dt className={styles.label}>
              GPU Model
            </dt>

            <dd className={styles.value}>
              {node.gpuModel}
            </dd>
          </div>

          <div className={styles.detail}>
            <dt className={styles.label}>
              GPU Count
            </dt>

            <dd className={styles.value}>
              {node.gpuCount}
            </dd>
          </div>

          <div
              className={`${styles.detail} ${
                  hasActiveFault
                      ? styles.faultDetail
                      : ""
              }`}
          >
            <dt className={styles.label}>
              Active Fault
            </dt>

            <dd className={styles.value}>
              {node.activeFault}
            </dd>
          </div>
        </dl>

        {node.status === "Provisioning" && (
            <p
                className={styles.info}
                role="status"
            >
              Node is being provisioned...
            </p>
        )}

        <div className={styles.actions}>
          {canStart && (
              <button
                  className={`${styles.actionButton} ${styles.startButton}`}
                  type="button"
                  onClick={() =>
                      dispatch(startNode(node.id))
                  }
                  disabled={actionLoading}
              >
                {actionLoading
                    ? "Starting..."
                    : "Start"}
              </button>
          )}

          {canStop && (
              <button
                  className={`${styles.actionButton} ${styles.stopButton}`}
                  type="button"
                  onClick={() =>
                      dispatch(stopNode(node.id))
                  }
                  disabled={actionLoading}
              >
                {actionLoading
                    ? "Stopping..."
                    : "Stop"}
              </button>
          )}

          {canRestart && (
              <button
                  className={`${styles.actionButton} ${styles.restartButton}`}
                  type="button"
                  onClick={() =>
                      dispatch(
                          restartNode(node.id),
                      )
                  }
                  disabled={actionLoading}
              >
                {actionLoading
                    ? "Restarting..."
                    : "Restart"}
              </button>
          )}
        </div>
      </article>
  );
}