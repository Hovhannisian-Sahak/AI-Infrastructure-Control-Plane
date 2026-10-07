"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import type { ComputeNode } from "@/lib/api/models/computeNode";
import type { HealthCheck } from "@/lib/api/models/healthCheck";

import {
    HEALTH_TIME_RANGES,
    type HealthTimeRange,
    getHealthRangeStart,
} from "@/lib/health/healthTimeRange";
import { deriveHealthAlerts } from "@/lib/health/healthAlerts";

import HealthSparkline from "@/components/health/HealthSparkline";
import HealthAlerts from "@/components/health/HealthAlerts";

import styles from "./HealthDashboard.module.css";

type HealthFilter =
    | "all"
    | "healthy"
    | "unhealthy"
    | "no-data";

type HealthDashboardProps = {
    nodes: ComputeNode[];
    historyByNodeId: Record<string, HealthCheck[]>;
    latestByNodeId: Record<
        string,
        HealthCheck | null | undefined
    >;
    loadingByNodeId: Record<string, boolean>;
};

type NodeHealthStatus =
    | "healthy"
    | "unhealthy"
    | "no-data";

function getNodeHealthStatus(
    latest: HealthCheck | null | undefined,
): NodeHealthStatus {
    if (!latest) {
        return "no-data";
    }

    return latest.isHealthy
        ? "healthy"
        : "unhealthy";
}

function formatValue(
    value: number | null | undefined,
    suffix: string,
) {
    if (value === null || value === undefined) {
        return "—";
    }

    return `${value.toFixed(1)}${suffix}`;
}

export default function HealthDashboard({
                                            nodes,
                                            historyByNodeId,
                                            latestByNodeId,
                                            loadingByNodeId,
                                        }: HealthDashboardProps) {
    const [filter, setFilter] =
        useState<HealthFilter>("all");

    const [range, setRange] =
        useState<HealthTimeRange>("24h");

    const nodesWithHealth = useMemo(
        () =>
            nodes.map(node => {
                const latest =
                    latestByNodeId[node.id] ?? null;

                const history =
                    historyByNodeId[node.id] ?? [];

                const filteredHistory =
                    history.filter(
                        item =>
                            new Date(
                                item.checkedAt,
                            ).getTime() >=
                            getHealthRangeStart(
                                range,
                            ).getTime(),
                    );

                return {
                    node,
                    latest,
                    history: filteredHistory,
                    status:
                        getNodeHealthStatus(latest),
                };
            }),
        [
            nodes,
            historyByNodeId,
            latestByNodeId,
            range,
        ],
    );

    const summary = useMemo(() => {
        let healthy = 0;
        let unhealthy = 0;
        let noData = 0;

        nodesWithHealth.forEach(item => {
            if (item.status === "healthy") {
                healthy++;
            } else if (
                item.status === "unhealthy"
            ) {
                unhealthy++;
            } else {
                noData++;
            }
        });

        return {
            healthy,
            unhealthy,
            noData,
            total: nodesWithHealth.length,
        };
    }, [nodesWithHealth]);

    const alerts = useMemo(
        () => deriveHealthAlerts(nodes, latestByNodeId),
        [nodes, latestByNodeId],
    );

    const visibleNodes = useMemo(
        () =>
            nodesWithHealth.filter(item => {
                if (filter === "all") {
                    return true;
                }

                return item.status === filter;
            }),
        [filter, nodesWithHealth],
    );

    return (
        <div className={styles.dashboard}>
            <div className={styles.summary}>
                <article className={styles.summaryCard}>
          <span className={styles.summaryLabel}>
            Total Nodes
          </span>

                    <strong className={styles.summaryValue}>
                        {summary.total}
                    </strong>
                </article>

                <article className={styles.summaryCard}>
          <span className={styles.summaryLabel}>
            Healthy
          </span>

                    <strong
                        className={`${styles.summaryValue} ${styles.healthyValue}`}
                    >
                        {summary.healthy}
                    </strong>
                </article>

                <article className={styles.summaryCard}>
          <span className={styles.summaryLabel}>
            Unhealthy
          </span>

                    <strong
                        className={`${styles.summaryValue} ${styles.unhealthyValue}`}
                    >
                        {summary.unhealthy}
                    </strong>
                </article>

                <article className={styles.summaryCard}>
          <span className={styles.summaryLabel}>
            No Data
          </span>

                    <strong className={styles.summaryValue}>
                        {summary.noData}
                    </strong>
                </article>
            </div>

            <HealthAlerts alerts={alerts} />

            <div className={styles.controls}>
                <div>
          <span className={styles.controlLabel}>
            Health status
          </span>

                    <div className={styles.buttons}>
                        {(
                            [
                                ["all", "All"],
                                ["healthy", "Healthy"],
                                ["unhealthy", "Unhealthy"],
                                ["no-data", "No Data"],
                            ] as const
                        ).map(([value, label]) => (
                            <button
                                key={value}
                                type="button"
                                className={
                                    filter === value
                                        ? styles.activeButton
                                        : styles.button
                                }
                                aria-pressed={
                                    filter === value
                                }
                                onClick={() =>
                                    setFilter(value)
                                }
                            >
                                {label}
                            </button>
                        ))}
                    </div>
                </div>

                <div>
          <span className={styles.controlLabel}>
            Time range
          </span>

                    <div className={styles.buttons}>
                        {HEALTH_TIME_RANGES.map(
                            option => (
                                <button
                                    key={option.value}
                                    type="button"
                                    className={
                                        range === option.value
                                            ? styles.activeButton
                                            : styles.button
                                    }
                                    aria-pressed={
                                        range === option.value
                                    }
                                    onClick={() =>
                                        setRange(option.value)
                                    }
                                >
                                    {option.label}
                                </button>
                            ),
                        )}
                    </div>
                </div>
            </div>

            <section className={styles.nodesSection}>
                <div className={styles.sectionHeader}>
                    <div>
                        <h2>Node Health</h2>

                        <p>
                            {visibleNodes.length} of{" "}
                            {summary.total} nodes
                        </p>
                    </div>
                </div>

                {visibleNodes.length === 0 ? (
                    <div className={styles.empty}>
                        <strong>
                            No nodes match this filter.
                        </strong>

                        <p>
                            Try selecting a different health
                            status.
                        </p>
                    </div>
                ) : (
                    <div className={styles.tableWrapper}>
                        <table className={styles.table}>
                            <thead>
                            <tr>
                                <th>Node</th>
                                <th>Status</th>
                                <th>CPU</th>
                                <th>GPU</th>
                                <th>Temperature</th>
                                <th>Trend</th>
                                <th />
                            </tr>
                            </thead>

                            <tbody>
                            {visibleNodes.map(
                                ({
                                     node,
                                     latest,
                                     history,
                                     status,
                                 }) => {
                                    const loading =
                                        loadingByNodeId[
                                            node.id
                                            ] ?? false;

                                    return (
                                        <tr key={node.id}>
                                            <td>
                                                <div
                                                    className={
                                                        styles.nodeInfo
                                                    }
                                                >
                                                    <strong>
                                                        {node.name}
                                                    </strong>

                                                    <span>
                              {node.gpuModel}
                            </span>
                                                </div>
                                            </td>

                                            <td>
                          <span
                              className={`${styles.status} ${
                                  styles[
                                      `${status}Status`
                                      ]
                              }`}
                          >
                            {status ===
                            "healthy"
                                ? "Healthy"
                                : status ===
                                "unhealthy"
                                    ? "Unhealthy"
                                    : "No Data"}
                          </span>
                                            </td>

                                            <td>
                                                {latest
                                                    ? formatValue(
                                                        latest.cpuUsagePercent,
                                                        "%",
                                                    )
                                                    : "—"}
                                            </td>

                                            <td>
                                                {latest
                                                    ? formatValue(
                                                        latest.gpuUsagePercent,
                                                        "%",
                                                    )
                                                    : "—"}
                                            </td>

                                            <td>
                                                {latest
                                                    ? formatValue(
                                                        latest.gpuTemperatureCelsius,
                                                        "°C",
                                                    )
                                                    : "—"}
                                            </td>

                                            <td>
                                                {loading &&
                                                history.length ===
                                                0 ? (
                                                    <span
                                                        className={
                                                            styles.loading
                                                        }
                                                    >
                              Loading...
                            </span>
                                                ) : (
                                                    <div
                                                        className={
                                                            styles.sparkline
                                                        }
                                                    >
                                                        <HealthSparkline
                                                            values={history.map(
                                                                item =>
                                                                    item.cpuUsagePercent,
                                                            )}
                                                            min={0}
                                                            max={100}
                                                            ariaLabel={`${node.name} CPU trend`}
                                                        />
                                                    </div>
                                                )}
                                            </td>

                                            <td>
                                                <Link
                                                    href={`/nodes/${node.id}`}
                                                    className={
                                                        styles.detailsLink
                                                    }
                                                >
                                                    Details
                                                </Link>
                                            </td>
                                        </tr>
                                    );
                                },
                            )}
                            </tbody>
                        </table>
                    </div>
                )}
            </section>
        </div>
    );
}