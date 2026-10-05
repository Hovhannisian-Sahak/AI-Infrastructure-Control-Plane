"use client";

import type {
    Incident,
    IncidentSeverity,
} from "@/lib/api/models/incident";
import styles from "./IncidentCard.module.css";

type IncidentCardProps = {
    incident: Incident;
};

const severityClassMap: Record<
    IncidentSeverity,
    string
> = {
    Low: styles.low,
    Medium: styles.medium,
    High: styles.high,
    Critical: styles.critical,
};

export default function IncidentCard({
                                         incident,
                                     }: IncidentCardProps) {
    return (
        <article className={styles.card}>
            <div className={styles.header}>
                <div className={styles.titleGroup}>
                    <h3 className={styles.title}>
                        {incident.title}
                    </h3>

                    <p className={styles.nodeId}>
                        Node: {incident.computeNodeId}
                    </p>
                </div>

                <span
                    className={`${styles.severity} ${severityClassMap[incident.severity]}`}
                >
          {incident.severity}
        </span>
            </div>

            {incident.description && (
                <p className={styles.description}>
                    {incident.description}
                </p>
            )}

            <div className={styles.footer}>
        <span className={styles.status}>
          {incident.status}
        </span>

                <time
                    className={styles.date}
                    dateTime={incident.createdAt}
                >
                    {new Date(incident.createdAt).toLocaleString()}
                </time>
            </div>
        </article>
    );
}