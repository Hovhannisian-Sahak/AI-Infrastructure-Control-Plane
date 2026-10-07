"use client";

import { useEffect, useState } from "react";

import {
    useAppDispatch,
    useAppSelector,
} from "@/store/hooks";

import { fetchNodes } from "@/store/slices/nodesSlice";

import {
    fetchHealthHistory,
} from "@/store/slices/healthSlice";

import HealthDashboard from "@/components/health/HealthDashboard";
import { getHealthMonitoringNodeIds } from "@/lib/health/healthMonitoring";
import {
    type HealthTimeRange,
    getHealthRangeBounds,
} from "@/lib/health/healthTimeRange";

import styles from "./page.module.css";

export default function HealthPage() {
    const dispatch = useAppDispatch();
    const [range, setRange] =
        useState<HealthTimeRange>("24h");

    const nodes = useAppSelector(
        state => state.nodes.nodes,
    );

    const historyByNodeId = useAppSelector(
        state => state.health.historyByNodeId,
    );

    const latestByNodeId = useAppSelector(
        state => state.health.latestByNodeId,
    );

    const loadingByNodeId = useAppSelector(
        state => state.health.loadingByNodeId,
    );

    const monitoredNodeIds =
        getHealthMonitoringNodeIds(nodes, true);
    const monitoredNodeKey =
        monitoredNodeIds.join(",");
    const allNodeKey = nodes
        .map(node => node.id)
        .sort()
        .join(",");

    useEffect(() => {
        dispatch(fetchNodes());
    }, [dispatch]);

    useEffect(() => {
        if (!allNodeKey) {
            return;
        }

        const { from, to } = getHealthRangeBounds(range);

        for (const nodeId of allNodeKey.split(",")) {
            void dispatch(
                fetchHealthHistory({
                    nodeId,
                    limit: 100,
                    from: from.toISOString(),
                    to: to.toISOString(),
                }),
            );
        }
    }, [dispatch, allNodeKey, range]);

    useEffect(() => {
        if (!monitoredNodeKey) {
            return;
        }

        const nodeIds =
            monitoredNodeKey.split(",");

        const loadHealth = () => {
            const { from, to } = getHealthRangeBounds(range);

            for (const nodeId of nodeIds) {
                void dispatch(
                    fetchHealthHistory({
                        nodeId,
                        limit: 100,
                        from: from.toISOString(),
                        to: to.toISOString(),
                    }),
                );
            }
        };

        const intervalId = setInterval(
            loadHealth,
            10_000,
        );

        return () => {
            clearInterval(intervalId);
        };
    }, [
        dispatch,
        monitoredNodeKey,
        range,
    ]);

    return (
        <main className={styles.page}>
            <div className={styles.container}>
                <header className={styles.header}>
                    <div>
                        <p className={styles.eyebrow}>
                            Fleet Monitoring
                        </p>

                        <h1>Health Overview</h1>

                        <p>
                            Monitor the health of your BeeCloud
                            compute nodes.
                        </p>
                    </div>
                </header>

                <HealthDashboard
                    nodes={nodes}
                    historyByNodeId={historyByNodeId}
                    latestByNodeId={latestByNodeId}
                    loadingByNodeId={loadingByNodeId}
                    onRangeChange={setRange}
                />
            </div>
        </main>
    );
}