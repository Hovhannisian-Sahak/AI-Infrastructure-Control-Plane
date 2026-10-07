"use client";

import type { ComputeNode } from "@/lib/api/models/computeNode";
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
import { isHealthMonitored } from "@/lib/health/healthMonitoring";
import styles from "./NodeCard.module.css";

type NodeCardProps = {
    node: ComputeNode;
};

const EMPTY_HEALTH_HISTORY: HealthCheck[] = [];

export default function NodeCard({ node }: NodeCardProps) {
    const dispatch = useAppDispatch();

    const actionLoading = useAppSelector(
        (state) =>
            state.nodes.actionLoadingByNodeId[node.id] ?? false,
    );

    const deleting =
        stateDeletingNodeId(useAppSelector((state) => state.nodes.deletingNodeId), node.id);

    const deleteError = useAppSelector(
        (state) =>
            state.nodes.deleteErrorByNodeId[node.id] ?? null,
    );

    const healthHistory = useAppSelector(
        (state) =>
            state.health.historyByNodeId[node.id] ??
            EMPTY_HEALTH_HISTORY,
    );

    const latestHealth =
        healthHistory.length > 0
            ? healthHistory[healthHistory.length - 1]
            : null;

    const cpuHistory = healthHistory.map(
        (health) => health.cpuUsagePercent,
    );

    const temperatureHistory = healthHistory.map(
        (health) => health.gpuTemperatureCelsius,
    );

    const isProvisioning = node.status === "Provisioning";
    const isMonitored = isHealthMonitored(node.status);

    const canStart =
        node.status === "Available" ||
        node.status === "Stopped";

    const canStop = node.status === "Running";

    const canRestart =
        node.status === "Running" ||
        node.status === "Unhealthy" ||
        node.status === "Quarantined";

    const handleStart = () => {
        void dispatch(startNode(node.id));
    };

    const handleStop = () => {
        void dispatch(stopNode(node.id));
    };

    const handleRestart = () => {
        void dispatch(restartNode(node.id));
    };

    const handleDelete = () => {
        if (
            window.confirm(
                `Are you sure you want to delete "${node.name}"?`,
            )
        ) {
            void dispatch(deleteNode(node.id));
        }
    };

    return (
        <article className={styles.card}>
            <div className={styles.header}>
                <div className={styles.titleGroup}>
                    <h2 className={styles.title}>{node.name}</h2>

                    <span className={styles.id}>
            {node.id}
          </span>
                </div>

                <span
                    className={`${styles.status} ${
                        styles[`status${node.status}`] ?? ""
                    }`}
                >
          {node.status}
        </span>
            </div>

            <div className={styles.details}>
                <div className={styles.detail}>
          <span className={styles.label}>
            GPU Model
          </span>

                    <strong className={styles.value}>
                        {node.gpuModel}
                    </strong>
                </div>

                <div className={styles.detail}>
          <span className={styles.label}>
            GPU Count
          </span>

                    <strong className={styles.value}>
                        {node.gpuCount}
                    </strong>
                </div>

                <div className={styles.detail}>
          <span className={styles.label}>
            Active Fault
          </span>

                    <strong className={styles.value}>
                        {node.activeFault}
                    </strong>
                </div>
            </div>

            {isProvisioning && (
                <div className={styles.provisioning}>
          <span className={styles.provisioningTitle}>
            Provisioning
          </span>

                    <span className={styles.provisioningText}>
            Node is being provisioned. This page will
            refresh automatically.
          </span>
                </div>
            )}

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
                            {isMonitored
                                ? latestHealth.isHealthy
                                    ? "Healthy"
                                    : "Unhealthy"
                                : (
                                    <>
                                        Last result:{" "}
                                        {latestHealth.isHealthy
                                            ? "Healthy"
                                            : "Unhealthy"}
                                    </>
                                )}
                        </span>
                    )}
                </div>

                {!isMonitored && (
                    <p className={styles.monitoringNote}>
                        Not currently monitored
                    </p>
                )}

                {latestHealth ? (
                    <div className={styles.healthMetrics}>
                        <div className={styles.healthMetric}>
                            <div className={styles.healthMetricHeader}>
                <span className={styles.label}>
                  CPU Usage
                </span>

                                <strong className={styles.value}>
                                    {latestHealth.cpuUsagePercent !== null
                                        ? `${latestHealth.cpuUsagePercent.toFixed(1)}%`
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
                            <div className={styles.healthMetricHeader}>
                <span className={styles.label}>
                  GPU Temperature
                </span>

                                <strong className={styles.value}>
                                    {latestHealth.gpuTemperatureCelsius !== null
                                        ? `${latestHealth.gpuTemperatureCelsius.toFixed(1)}°C`
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

            {deleteError && (
                <p className={styles.error} role="alert">
                    {deleteError}
                </p>
            )}

            <div className={styles.actions}>
                {canStart && (
                    <button
                        type="button"
                        className={styles.actionButton}
                        disabled={actionLoading || deleting}
                        onClick={handleStart}
                    >
                        {actionLoading ? "Starting..." : "Start"}
                    </button>
                )}

                {canStop && (
                    <button
                        type="button"
                        className={styles.actionButton}
                        disabled={actionLoading || deleting}
                        onClick={handleStop}
                    >
                        {actionLoading ? "Stopping..." : "Stop"}
                    </button>
                )}

                {canRestart && (
                    <button
                        type="button"
                        className={styles.actionButton}
                        disabled={actionLoading || deleting}
                        onClick={handleRestart}
                    >
                        {actionLoading ? "Restarting..." : "Restart"}
                    </button>
                )}

                <button
                    type="button"
                    className={`${styles.actionButton} ${styles.deleteButton}`}
                    disabled={deleting || actionLoading}
                    onClick={handleDelete}
                >
                    {deleting ? "Deleting..." : "Delete"}
                </button>
            </div>
        </article>
    );
}

function stateDeletingNodeId(
    deletingNodeId: string | null,
    nodeId: string,
): boolean {
    return deletingNodeId === nodeId;
}