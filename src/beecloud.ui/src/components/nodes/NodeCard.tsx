"use client";

import type {
    ComputeNode,
    NodeStatus,
} from "@/lib/api/models/computeNode";
import {
    useAppDispatch,
    useAppSelector,
} from "@/store/hooks";
import {
    startNode,
    stopNode,
} from "@/store/slices/nodesSlice";
import styles from "./NodeCard.module.css";

type NodeCardProps = {
    node: ComputeNode;
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

export default function NodeCard({ node }: NodeCardProps) {
    const dispatch = useAppDispatch();

    const actionLoading = useAppSelector(
        (state) => state.nodes.actionLoadingByNodeId[node.id] ?? false,
    );

    const canStart =
        node.status === "Available" ||
        node.status === "Stopped";

    const canStop = node.status === "Running";

    return (
        <article className={styles.card}>
            <div className={styles.header}>
                <h2 className={styles.title}>{node.name}</h2>

                <span
                    className={`${styles.status} ${statusClassMap[node.status]}`}
                >
          {node.status}
        </span>
            </div>

            <dl className={styles.details}>
                <div className={styles.detail}>
                    <dt className={styles.label}>GPU Model</dt>
                    <dd className={styles.value}>{node.gpuModel}</dd>
                </div>

                <div className={styles.detail}>
                    <dt className={styles.label}>GPU Count</dt>
                    <dd className={styles.value}>{node.gpuCount}</dd>
                </div>

                <div className={styles.detail}>
                    <dt className={styles.label}>Active Fault</dt>
                    <dd className={styles.value}>{node.activeFault}</dd>
                </div>
            </dl>

            <div className={styles.actions}>
                {canStart && (
                    <button
                        type="button"
                        onClick={() => dispatch(startNode(node.id))}
                        disabled={actionLoading}
                    >
                        {actionLoading ? "Starting..." : "Start"}
                    </button>
                )}

                {canStop && (
                    <button
                        type="button"
                        onClick={() => dispatch(stopNode(node.id))}
                        disabled={actionLoading}
                    >
                        {actionLoading ? "Stopping..." : "Stop"}
                    </button>
                )}
            </div>
        </article>
    );
}