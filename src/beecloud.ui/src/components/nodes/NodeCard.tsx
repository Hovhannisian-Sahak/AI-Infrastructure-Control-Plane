import type { ComputeNode } from "@/lib/api/models/computeNode";
import styles from "./NodeCard.module.css";

type NodeCardProps = {
    node: ComputeNode;
};

export default function NodeCard({ node }: NodeCardProps) {
    return (
        <article className={styles.card}>
            <h2 className={styles.title}>{node.name}</h2>

            <p className={styles.info}>
                GPU: {node.gpuModel} × {node.gpuCount}
            </p>

            <p className={styles.status}>
                Status: {node.status}
            </p>

            <p className={styles.info}>
                Fault: {node.activeFault}
            </p>
        </article>
    );
}