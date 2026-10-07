"use client";

import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";
import type { NodeMetric } from "@/lib/api/models/nodeMetric";
import Link from "next/link";
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
const EMPTY_METRICS_HISTORY: NodeMetric[] = [];

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

    const metricsHistory = useAppSelector(
        (state) =>
            state.metrics.historyByNodeId[node.id] ??
            EMPTY_METRICS_HISTORY,
    );
    const metricsError = useAppSelector(
        (state) => state.metrics.errorByNodeId[node.id] ?? null,
    );
    const latestMetric =
        metricsHistory.length > 0
            ? metricsHistory[metricsHistory.length - 1]
            : null;

    const cpuHistory = metricsHistory.map(
        (metric) => metric.cpuUsagePercent,
    );

    const gpuHistory = metricsHistory.map(
        (metric) => metric.gpuUsagePercent,
    );

    const temperatureHistory = metricsHistory.map(
        (metric) => metric.gpuTemperatureCelsius,
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
                    <h2 className={styles.title}>
                        <Link
                            className={styles.titleLink}
                            href={`/nodes/${node.id}`}
                        >
                            {node.name}
                        </Link>
                    </h2>

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

                {!latestHealth && (
                    <p className={styles.noHealthData}>
                        No health check yet
                    </p>
                )}
            </section>

            <section className={styles.healthSection}>
                <div className={styles.healthHeader}>
                    <h3 className={styles.healthTitle}>Resource Metrics</h3>
                </div>
                {latestMetric ? (
                    <div className={styles.resourceMetrics}>
                        <Metric
                            label="CPU Usage"
                            value={`${latestMetric.cpuUsagePercent.toFixed(1)}%`}
                            values={cpuHistory}
                            ariaLabel="CPU usage trend"
                        />
                        <Metric
                            label="GPU Usage"
                            value={`${latestMetric.gpuUsagePercent.toFixed(1)}%`}
                            values={gpuHistory}
                            ariaLabel="GPU usage trend"
                        />
                        <Metric
                            label="GPU Temperature"
                            value={`${latestMetric.gpuTemperatureCelsius.toFixed(1)}°C`}
                            values={temperatureHistory}
                            min={0}
                            max={120}
                            ariaLabel="GPU temperature trend"
                        />
                    </div>
                ) : (
                    <p className={styles.noHealthData}>
                        {metricsError
                            ? `Resource metrics unavailable: ${metricsError}`
                            : "No resource metrics available"}
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

type MetricProps = {
    label: string;
    value: string;
    values: number[];
    ariaLabel: string;
    min?: number;
    max?: number;
};

function Metric({
    label,
    value,
    values,
    ariaLabel,
    min = 0,
    max = 100,
}: MetricProps) {
    return (
        <div className={styles.healthMetric}>
            <div className={styles.healthMetricHeader}>
                <span className={styles.label}>{label}</span>
                <strong className={styles.value}>{value}</strong>
            </div>
            <HealthSparkline
                values={values}
                min={min}
                max={max}
                ariaLabel={ariaLabel}
            />
        </div>
    );
}

function stateDeletingNodeId(
    deletingNodeId: string | null,
    nodeId: string,
): boolean {
    return deletingNodeId === nodeId;
}