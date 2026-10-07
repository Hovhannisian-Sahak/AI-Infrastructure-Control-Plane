"use client";

import type { HealthCheck } from "@/lib/api/models/healthCheck";

import HealthSparkline from "@/components/health/HealthSparkline";
import CursorPagination from "@/components/common/CursorPagination";

import styles from "./HealthHistory.module.css";

type HealthHistoryProps = {
    history: HealthCheck[];
    tableHistory?: HealthCheck[];
    loading?: boolean;
    tableLoading?: boolean;
    tableError?: string | null;
    nextCursor?: string | null;
    previousCursor?: string | null;
    onTableNavigate?: (cursor: string, previous: boolean) => void;
};

function formatTimestamp(value: string) {
    return new Date(value).toLocaleString();
}

function formatValue(
    value: number | null,
    suffix: string,
) {
    return value === null
        ? "N/A"
        : `${value.toFixed(1)}${suffix}`;
}

export default function HealthHistory({
                                          history,
                                          tableHistory,
                                          loading = false,
                                          tableLoading = false,
                                          tableError = null,
                                          nextCursor = null,
                                          previousCursor = null,
                                          onTableNavigate,
                                      }: HealthHistoryProps) {
    const tableItems = tableHistory ?? history;
    const newestFirstItems = tableHistory
        ? tableItems
        : [...tableItems].reverse();

    if (loading && history.length === 0 && tableItems.length === 0) {
        return (
            <section className={styles.section}>
                <h2 className={styles.title}>
                    Health History
                </h2>

                <p className={styles.message}>
                    Loading health history...
                </p>
            </section>
        );
    }

    if (history.length === 0 && tableItems.length === 0) {
        return (
            <section className={styles.section}>
                <h2 className={styles.title}>
                    Health History
                </h2>

                <p className={styles.message}>
                    No health checks available for this node.
                </p>
            </section>
        );
    }

    const latest = history[history.length - 1] ?? tableItems[0];

    const cpuHistory = history.map(
        item => item.cpuUsagePercent,
    );

    const gpuHistory = history.map(
        item => item.gpuUsagePercent,
    );

    const temperatureHistory = history.map(
        item => item.gpuTemperatureCelsius,
    );

    return (
        <section className={styles.section}>
            <div className={styles.header}>
                <div>
                    <h2 className={styles.title}>
                        Health History
                    </h2>

                    <p className={styles.subtitle}>
                        {history.length || tableItems.length} displayed health checks
                    </p>
                </div>

                <span
                    className={`${styles.status} ${
                        latest.isHealthy
                            ? styles.healthy
                            : styles.unhealthy
                    }`}
                >
          {latest.isHealthy
              ? "Healthy"
              : "Unhealthy"}
        </span>
            </div>

            <div className={styles.metrics}>
                <article className={styles.metric}>
                    <div className={styles.metricHeader}>
                        <span>CPU Usage</span>
                        <strong>
                            {formatValue(
                                latest.cpuUsagePercent,
                                "%",
                            )}
                        </strong>
                    </div>

                    <HealthSparkline
                        values={cpuHistory}
                        min={0}
                        max={100}
                        ariaLabel="CPU usage history"
                    />
                </article>

                <article className={styles.metric}>
                    <div className={styles.metricHeader}>
                        <span>GPU Usage</span>
                        <strong>
                            {formatValue(
                                latest.gpuUsagePercent,
                                "%",
                            )}
                        </strong>
                    </div>

                    <HealthSparkline
                        values={gpuHistory}
                        min={0}
                        max={100}
                        ariaLabel="GPU usage history"
                    />
                </article>

                <article className={styles.metric}>
                    <div className={styles.metricHeader}>
                        <span>GPU Temperature</span>
                        <strong>
                            {formatValue(
                                latest.gpuTemperatureCelsius,
                                "°C",
                            )}
                        </strong>
                    </div>

                    <HealthSparkline
                        values={temperatureHistory}
                        min={50}
                        max={100}
                        ariaLabel="GPU temperature history"
                    />
                </article>
            </div>

            <div className={styles.tableWrapper}>
                {tableError && <p role="alert">{tableError}</p>}
                <table className={styles.table}>
                    <thead>
                    <tr>
                        <th>Checked At</th>
                        <th>Status</th>
                        <th>CPU</th>
                        <th>GPU</th>
                        <th>Temperature</th>
                    </tr>
                    </thead>

                    <tbody>
                    {newestFirstItems
                        .map(item => (
                            <tr key={item.id}>
                                <td>
                                    {formatTimestamp(
                                        item.checkedAt,
                                    )}
                                </td>

                                <td>
                    <span
                        className={
                            item.isHealthy
                                ? styles.tableHealthy
                                : styles.tableUnhealthy
                        }
                    >
                      {item.isHealthy
                          ? "Healthy"
                          : "Unhealthy"}
                    </span>
                                </td>

                                <td>
                                    {formatValue(
                                        item.cpuUsagePercent,
                                        "%",
                                    )}
                                </td>

                                <td>
                                    {formatValue(
                                        item.gpuUsagePercent,
                                        "%",
                                    )}
                                </td>

                                <td>
                                    {formatValue(
                                        item.gpuTemperatureCelsius,
                                        "°C",
                                    )}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            {onTableNavigate && (
                <CursorPagination
                    nextCursor={nextCursor}
                    previousCursor={previousCursor}
                    loading={tableLoading}
                    ariaLabel="Health history pages"
                    onNavigate={onTableNavigate}
                />
            )}
        </section>
    );
}