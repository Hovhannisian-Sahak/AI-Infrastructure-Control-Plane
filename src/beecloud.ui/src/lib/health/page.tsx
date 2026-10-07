"use client";

import { useEffect } from "react";

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

import styles from "./page.module.css";

export default function HealthPage() {
    const dispatch = useAppDispatch();

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
    const unmonitoredNodeIds =
        getHealthMonitoringNodeIds(nodes, false);

    const monitoredNodeKey =
        monitoredNodeIds.join(",");
    const unmonitoredNodeKey =
        unmonitoredNodeIds.join(",");

    useEffect(() => {
        dispatch(fetchNodes());
    }, [dispatch]);

    useEffect(() => {
        if (!unmonitoredNodeKey) {
            return;
        }

        for (const nodeId of unmonitoredNodeKey.split(",")) {
            void dispatch(
                fetchHealthHistory({
                    nodeId,
                    limit: 100,
                }),
            );
        }
    }, [dispatch, unmonitoredNodeKey]);

    useEffect(() => {
        if (!monitoredNodeKey) {
            return;
        }

        const nodeIds =
            monitoredNodeKey.split(",");

        const loadHealth = () => {
            for (const nodeId of nodeIds) {
                void dispatch(
                    fetchHealthHistory({
                        nodeId,
                        limit: 100,
                    }),
                );
            }
        };

        loadHealth();

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
                />
            </div>
        </main>
    );
}