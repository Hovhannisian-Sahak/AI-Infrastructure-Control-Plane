"use client";

import type { NodeMetric } from "@/lib/api/models/nodeMetric";
import HealthSparkline from "@/components/health/HealthSparkline";
import CursorPagination from "@/components/common/CursorPagination";

import styles from "./MetricsHistory.module.css";

type MetricsHistoryProps = {
    history: NodeMetric[];
    loading?: boolean;
    error?: string | null;
    tableHistory?: NodeMetric[];
    tableLoading?: boolean;
    tableError?: string | null;
    nextCursor?: string | null;
    previousCursor?: string | null;
    onTableNavigate?: (cursor: string, previous: boolean) => void;
};

function formatTimestamp(value: string) {
    return new Date(value).toLocaleString();
}

export default function MetricsHistory({
    history,
    loading = false,
    error = null,
    tableHistory,
    tableLoading = false,
    tableError = null,
    nextCursor = null,
    previousCursor = null,
    onTableNavigate,
}: MetricsHistoryProps) {
    const tableItems = tableHistory ?? history;
    const latest = history[history.length - 1] ?? tableItems[0];
    const newestFirstItems = tableHistory
        ? tableItems
        : [...tableItems].reverse();

    return (
        <section className={styles.section} aria-labelledby="node-metrics-title">
            <header className={styles.header}>
                <div>
                    <h2 id="node-metrics-title">Node Metrics</h2>
                    <p>
                        Resource telemetry from the selected time range
                    </p>
                </div>
                {latest && (
                    <span className={styles.timestamp}>
                        Latest: {formatTimestamp(latest.recordedAt)}
                    </span>
                )}
            </header>

            {(error || tableError) && (
                <p className={styles.error} role="alert">
                    {error || tableError}
                </p>
            )}

            {loading && history.length === 0 && tableItems.length === 0 ? (
                <p className={styles.message}>Loading node metrics...</p>
            ) : history.length === 0 && tableItems.length === 0 ? (
                <p className={styles.message}>
                    No resource metrics available for this time range.
                </p>
            ) : (
                <>
                    {history.length > 0 && latest && (
                        <div className={styles.metrics}>
                            <Metric
                                label="CPU Usage"
                                value={`${latest.cpuUsagePercent.toFixed(1)}%`}
                                values={history.map(item => item.cpuUsagePercent)}
                                ariaLabel="CPU usage metrics history"
                            />
                            <Metric
                                label="GPU Usage"
                                value={`${latest.gpuUsagePercent.toFixed(1)}%`}
                                values={history.map(item => item.gpuUsagePercent)}
                                ariaLabel="GPU usage metrics history"
                            />
                            <Metric
                                label="GPU Temperature"
                                value={`${latest.gpuTemperatureCelsius.toFixed(1)}°C`}
                                values={history.map(item => item.gpuTemperatureCelsius)}
                                min={0}
                                max={120}
                                ariaLabel="GPU temperature metrics history"
                            />
                        </div>
                    )}

                    {tableItems.length > 0 && (
                        <div className={styles.tableWrapper}>
                            <table className={styles.table}>
                            <thead>
                                <tr>
                                    <th>Recorded At</th>
                                    <th>CPU</th>
                                    <th>GPU</th>
                                    <th>Temperature</th>
                                </tr>
                            </thead>
                            <tbody>
                                {newestFirstItems.map(metric => (
                                    <tr key={metric.id}>
                                        <td>{formatTimestamp(metric.recordedAt)}</td>
                                        <td>{metric.cpuUsagePercent.toFixed(1)}%</td>
                                        <td>{metric.gpuUsagePercent.toFixed(1)}%</td>
                                        <td>{metric.gpuTemperatureCelsius.toFixed(1)}°C</td>
                                    </tr>
                                ))}
                            </tbody>
                            </table>
                        </div>
                    )}
                    {tableItems.length === 0 && tableLoading && (
                        <p className={styles.message}>Loading metrics history...</p>
                    )}
                    {tableItems.length === 0 && !tableLoading && (
                        <p className={styles.message}>No metric readings in this time range.</p>
                    )}
                    {onTableNavigate && (
                        <CursorPagination
                            nextCursor={nextCursor}
                            previousCursor={previousCursor}
                            loading={tableLoading}
                            ariaLabel="Metrics history pages"
                            onNavigate={onTableNavigate}
                        />
                    )}
                </>
            )}
        </section>
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
        <article className={styles.metric}>
            <div className={styles.metricHeader}>
                <span>{label}</span>
                <strong>{value}</strong>
            </div>
            <HealthSparkline
                values={values}
                min={min}
                max={max}
                ariaLabel={ariaLabel}
            />
        </article>
    );
}
