"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams } from "next/navigation";

import {
    useAppDispatch,
    useAppSelector,
} from "@/store/hooks";

import {
    fetchNodes,
} from "@/store/slices/nodesSlice";

import {
    fetchHealthHistory,
} from "@/store/slices/healthSlice";
import { fetchNodeMetrics } from "@/store/slices/metricsSlice";
import { nodesApi } from "@/lib/api/nodesApi";
import type { CursorPageResponse } from "@/lib/api/models/pageResponse";
import type { HealthCheck } from "@/lib/api/models/healthCheck";
import type { NodeMetric } from "@/lib/api/models/nodeMetric";

import {
    selectHealthErrorByNodeId,
    selectHealthHistoryByNodeId,
    selectHealthLoadingByNodeId,
    selectLatestHealthByNodeId,
} from "@/store/selectors/healthSelectors";

import {
    HEALTH_TIME_RANGES,
    type HealthTimeRange,
    getHealthRangeBounds,
} from "@/lib/health/healthTimeRange";

import HealthHistory from "@/lib/health/HealthHistory";
import MetricsHistory from "@/components/health/MetricsHistory";
import { isHealthMonitored } from "@/lib/health/healthMonitoring";

import styles from "./NodeDetailPage.module.css";

export default function NodeDetailPage() {
    const params = useParams();

    const nodeId = (Array.isArray(params.nodeId)
        ? params.nodeId[0]
        : params.nodeId) ?? "";

    const dispatch = useAppDispatch();

    const node = useAppSelector(state =>
        state.nodes.nodes.find(
            item => item.id === nodeId,
        ),
    );

    const history =
        useAppSelector(state =>
            selectHealthHistoryByNodeId(
                state,
                nodeId,
            ),
        );

    const latestHealth =
        useAppSelector(state =>
            selectLatestHealthByNodeId(
                state,
                nodeId,
            ),
        );

    const loading =
        useAppSelector(state =>
            selectHealthLoadingByNodeId(
                state,
                nodeId,
            ),
        );

    const error =
        useAppSelector(state =>
            selectHealthErrorByNodeId(
                state,
                nodeId,
            ),
        );
    const metrics = useAppSelector(
        state => state.metrics.historyByNodeId[nodeId] ?? [],
    );
    const metricsLoading = useAppSelector(
        state => state.metrics.loadingByNodeId[nodeId] ?? false,
    );
    const metricsError = useAppSelector(
        state => state.metrics.errorByNodeId[nodeId] ?? null,
    );

    const [range, setRange] =
        useState<HealthTimeRange>("24h");
    const [healthTablePage, setHealthTablePage] =
        useState<CursorPageResponse<HealthCheck>>({
            items: [],
            nextCursor: null,
            previousCursor: null,
        });
    const [metricsTablePage, setMetricsTablePage] =
        useState<CursorPageResponse<NodeMetric>>({
            items: [],
            nextCursor: null,
            previousCursor: null,
        });
    const [healthTableRequest, setHealthTableRequest] =
        useState<{ cursor: string | null; previous: boolean }>({
            cursor: null,
            previous: false,
        });
    const [metricsTableRequest, setMetricsTableRequest] =
        useState<{ cursor: string | null; previous: boolean }>({
            cursor: null,
            previous: false,
        });
    const [healthTableLoading, setHealthTableLoading] = useState(true);
    const [metricsTableLoading, setMetricsTableLoading] = useState(true);
    const [healthTableError, setHealthTableError] = useState<string | null>(null);
    const [metricsTableError, setMetricsTableError] = useState<string | null>(null);

    useEffect(() => {
        if (!nodeId) {
            return;
        }

        if (!node) {
            dispatch(fetchNodes());
        }
    }, [dispatch, node, nodeId]);

    useEffect(() => {
        if (!nodeId) {
            return;
        }

        const { from, to } = getHealthRangeBounds(range);

        dispatch(
            fetchHealthHistory({
                nodeId,
                limit: 100,
                from: from.toISOString(),
                to: to.toISOString(),
            }),
        );
        dispatch(
            fetchNodeMetrics({
                nodeId,
                limit: 100,
                from: from.toISOString(),
                to: to.toISOString(),
            }),
        );
    }, [dispatch, nodeId, range]);

    useEffect(() => {
        if (!nodeId) return;

        let active = true;
        const { from, to } = getHealthRangeBounds(range);

        nodesApi.getHealthHistoryPage(nodeId, {
            from: from.toISOString(),
            to: to.toISOString(),
            cursor: healthTableRequest.cursor,
            previous: healthTableRequest.previous,
            limit: 25,
        }).then(result => {
            if (active) setHealthTablePage(result);
        }).catch(fetchError => {
            if (active) {
                setHealthTableError(
                    fetchError instanceof Error
                        ? fetchError.message
                        : "Failed to fetch health history page.",
                );
            }
        }).finally(() => {
            if (active) setHealthTableLoading(false);
        });

        return () => {
            active = false;
        };
    }, [nodeId, range, healthTableRequest]);

    useEffect(() => {
        if (!nodeId) return;

        let active = true;
        const { from, to } = getHealthRangeBounds(range);

        nodesApi.getNodeMetricsPage(nodeId, {
            from: from.toISOString(),
            to: to.toISOString(),
            cursor: metricsTableRequest.cursor,
            previous: metricsTableRequest.previous,
            limit: 25,
        }).then(result => {
            if (active) setMetricsTablePage(result);
        }).catch(fetchError => {
            if (active) {
                setMetricsTableError(
                    fetchError instanceof Error
                        ? fetchError.message
                        : "Failed to fetch metrics history page.",
                );
            }
        }).finally(() => {
            if (active) setMetricsTableLoading(false);
        });

        return () => {
            active = false;
        };
    }, [nodeId, range, metricsTableRequest]);

    useEffect(() => {
        if (!nodeId || !node || !isHealthMonitored(node.status)) {
            return;
        }

        let active = true;
        const loadMetrics = () => {
            const { from, to } = getHealthRangeBounds(range);
            void dispatch(
                fetchNodeMetrics({
                    nodeId,
                    limit: 100,
                    from: from.toISOString(),
                    to: to.toISOString(),
                }),
            );
            if (metricsTableRequest.cursor === null) {
                void nodesApi.getNodeMetricsPage(nodeId, {
                    from: from.toISOString(),
                    to: to.toISOString(),
                    limit: 25,
                }).then(result => {
                    if (!active) return;
                    setMetricsTablePage(result);
                    setMetricsTableError(null);
                }).catch(fetchError => {
                    if (active) {
                        setMetricsTableError(
                            fetchError instanceof Error
                                ? fetchError.message
                                : "Failed to refresh metrics history.",
                        );
                    }
                });
            }
        };

        const intervalId = setInterval(loadMetrics, 10_000);
        return () => {
            active = false;
            clearInterval(intervalId);
        };
    }, [dispatch, metricsTableRequest.cursor, node, nodeId, range]);

    if (!node) {
        return (
            <main className={styles.page}>
                <div className={styles.notFound}>
                    <h1>Node not found</h1>

                    <p>
                        The requested compute node could not
                        be found.
                    </p>

                    <Link
                        href="/"
                        className={styles.backLink}
                    >
                        Back to nodes
                    </Link>
                </div>
            </main>
        );
    }

    return (
        <main className={styles.page}>
            <div className={styles.container}>
                <Link
                    href="/"
                    className={styles.backLink}
                >
                    ← Back to nodes
                </Link>

                <header className={styles.header}>
                    <div>
                        <p className={styles.eyebrow}>
                            Compute Node
                        </p>

                        <h1 className={styles.title}>
                            {node.name}
                        </h1>

                        <p className={styles.id}>
                            {node.id}
                        </p>
                    </div>

                    <span
                        className={styles.status}
                    >
            {node.status}
          </span>
                </header>

                <section className={styles.details}>
                    <div>
            <span className={styles.label}>
              GPU Model
            </span>

                        <strong>
                            {node.gpuModel}
                        </strong>
                    </div>

                    <div>
            <span className={styles.label}>
              GPU Count
            </span>

                        <strong>
                            {node.gpuCount}
                        </strong>
                    </div>

                    <div>
            <span className={styles.label}>
              Active Fault
            </span>

                        <strong>
                            {node.activeFault}
                        </strong>
                    </div>

                    <div>
            <span className={styles.label}>
              Current Health
            </span>

                        <strong>
                            {latestHealth
                                ? latestHealth.isHealthy
                                    ? "Healthy"
                                    : "Unhealthy"
                                : "No data"}
                        </strong>
                    </div>
                </section>

                <section className={styles.healthHeader}>
                    <div data-testid="node-detail-page-header">
                        <h2>Health</h2>

                        <p>
                            Monitor recent health measurements
                            for this compute node.
                        </p>
                    </div>

                    <div
                        className={styles.rangeSelector}
                        aria-label="Health time range"
                    >
                        {HEALTH_TIME_RANGES.map(
                            option => (
                                <button
                                    key={option.value}
                                    type="button"
                                    className={
                                        range === option.value
                                            ? styles.rangeActive
                                            : styles.rangeButton
                                    }
                                    aria-pressed={
                                        range === option.value
                                    }
                                    onClick={() =>
                                        {
                                            setRange(option.value);
                                            setHealthTableLoading(true);
                                            setMetricsTableLoading(true);
                                            setHealthTableError(null);
                                            setMetricsTableError(null);
                                            setHealthTableRequest({
                                                cursor: null,
                                                previous: false,
                                            });
                                            setMetricsTableRequest({
                                                cursor: null,
                                                previous: false,
                                            });
                                        }
                                    }
                                >
                                    {option.label}
                                </button>
                            ),
                        )}
                    </div>
                </section>

                {error && (
                    <p
                        role="alert"
                        className={styles.error}
                    >
                        {error}
                    </p>
                )}

                <HealthHistory
                    history={history}
                    loading={loading}
                    tableHistory={healthTablePage.items}
                    tableLoading={healthTableLoading}
                    tableError={healthTableError}
                    nextCursor={healthTablePage.nextCursor}
                    previousCursor={healthTablePage.previousCursor}
                    onTableNavigate={(cursor, previous) =>
                        {
                            setHealthTableLoading(true);
                            setHealthTableError(null);
                            setHealthTableRequest({ cursor, previous });
                        }
                    }
                />
                <MetricsHistory
                    history={metrics}
                    loading={metricsLoading}
                    error={metricsError}
                    tableHistory={metricsTablePage.items}
                    tableLoading={metricsTableLoading}
                    tableError={metricsTableError}
                    nextCursor={metricsTablePage.nextCursor}
                    previousCursor={metricsTablePage.previousCursor}
                    onTableNavigate={(cursor, previous) =>
                        {
                            setMetricsTableLoading(true);
                            setMetricsTableError(null);
                            setMetricsTableRequest({ cursor, previous });
                        }
                    }
                />
            </div>
        </main>
    );
}