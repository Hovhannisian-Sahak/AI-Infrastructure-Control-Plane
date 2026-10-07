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
        if (!nodeId || !node || !isHealthMonitored(node.status)) {
            return;
        }

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
        };

        const intervalId = setInterval(loadMetrics, 10_000);
        return () => clearInterval(intervalId);
    }, [dispatch, node, nodeId, range]);

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
                    <div>
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
                                        setRange(option.value)
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
                />
                <MetricsHistory
                    history={metrics}
                    loading={metricsLoading}
                    error={metricsError}
                />
            </div>
        </main>
    );
}