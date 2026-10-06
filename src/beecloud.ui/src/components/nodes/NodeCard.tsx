"use client";

import type {
  ComputeNode,
  NodeStatus,
} from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";
import {
  useAppDispatch,
  useAppSelector,
} from "@/store/hooks";
import {
  deleteNode,
  restartNode,
  startNode,
  stopNode,
} from "@/store/slices/nodesSlice";
import HealthSparkline from "@/components/health/HealthSparkline";
import styles from "./NodeCard.module.css";

type NodeCardProps = {
  node: ComputeNode;
  healthHistory: HealthCheck[];
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
                                   healthHistory,
                                 }: NodeCardProps) {
  const dispatch = useAppDispatch();

  const actionLoading = useAppSelector(
      (state) =>
          state.nodes.actionLoadingByNodeId[node.id] ??
          false,
  );

  const deleting = useAppSelector(
      (state) =>
          state.nodes.deletingNodeId === node.id,
  );

  const deleteError = useAppSelector(
      (state) =>
          state.nodes.deleteErrorByNodeId[node.id] ??
          null,
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

  const latestHealth =
      healthHistory.length > 0
          ? healthHistory[healthHistory.length - 1]
          : null;

  const cpuHistory = healthHistory.map(
      (check) => check.cpuUsagePercent,
  );

  const temperatureHistory = healthHistory.map(
      (check) => check.gpuTemperatureCelsius,
  );

  const handleDelete = () => {
    const confirmed = window.confirm(
        `Are you sure you want to delete "${node.name}"?`,
    );

    if (!confirmed) {
      return;
    }

    dispatch(deleteNode(node.id));
  };

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

        <section className={styles.healthSection}>
          <div className={styles.healthHeader}>
            <h3 className={styles.healthTitle}>
              Health
            </h3>

            {latestHealth && (
                <span
                    className={`${styles.healthStatus} ${
                        latestHealth.isHealthy
                            ? styles.healthHealthy
                            : styles.healthUnhealthy
                    }`}
                >
              {latestHealth.isHealthy
                  ? "Healthy"
                  : "Unhealthy"}
            </span>
            )}
          </div>

          {latestHealth ? (
              <div className={styles.healthMetrics}>
                <div className={styles.healthMetric}>
                  <div
                      className={
                        styles.healthMetricHeader
                      }
                  >
                <span className={styles.label}>
                  CPU Usage
                </span>

                    <strong className={styles.value}>
                      {latestHealth.cpuUsagePercent !==
                      null
                          ? `${latestHealth.cpuUsagePercent.toFixed(
                              1,
                          )}%`
                          : "N/A"}
                    </strong>
                  </div>

                  <HealthSparkline
                      values={cpuHistory}
                      min={0}
                      max={100}
                      ariaLabel="CPU usage trend"
                  />
                </div>

                <div className={styles.healthMetric}>
                  <div
                      className={
                        styles.healthMetricHeader
                      }
                  >
                <span className={styles.label}>
                  GPU Temperature
                </span>

                    <strong className={styles.value}>
                      {latestHealth.gpuTemperatureCelsius !==
                      null
                          ? `${latestHealth.gpuTemperatureCelsius.toFixed(
                              1,
                          )}°C`
                          : "N/A"}
                    </strong>
                  </div>

                  <HealthSparkline
                      values={temperatureHistory}
                      min={50}
                      max={100}
                      ariaLabel="GPU temperature trend"
                  />
                </div>
              </div>
          ) : (
              <p className={styles.noHealthData}>
                No health check yet
              </p>
          )}
        </section>

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
                      dispatch(restartNode(node.id))
                  }
                  disabled={actionLoading}
              >
                {actionLoading
                    ? "Restarting..."
                    : "Restart"}
              </button>
          )}

          <button
              className={`${styles.actionButton} ${styles.deleteButton}`}
              type="button"
              onClick={handleDelete}
              disabled={
                  actionLoading || deleting
              }
          >
            {deleting
                ? "Deleting..."
                : "Delete"}
          </button>
        </div>

        {deleteError && (
            <p
                className={styles.error}
                role="alert"
            >
              {deleteError}
            </p>
        )}
      </article>
  );
}