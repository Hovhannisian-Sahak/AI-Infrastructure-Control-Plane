import type { ComputeNode } from "@/lib/api/models/computeNode";
import styles from "./NodeCard.module.css";

type NodeCardProps = {
    node: ComputeNode;
};

export default function NodeCard({ node }: NodeCardProps) {
    return (
        <article className={styles.card}>
            <div className={styles.header}>
                <h2 className={styles.title}>{node.name}</h2>

                <span className={styles.status}>
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