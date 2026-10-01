import type {
    ComputeNode,
    NodeStatus,
} from "@/lib/api/models/computeNode";
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
        </article>
    );
}