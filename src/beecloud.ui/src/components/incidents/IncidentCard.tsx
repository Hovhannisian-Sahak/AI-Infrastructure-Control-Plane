"use client";

import type {
    Incident,
    IncidentSeverity,
    IncidentStatus,
} from "@/lib/api/models/incident";
import Link from "next/link";
import styles from "./IncidentCard.module.css";

type IncidentCardProps = {
    incident: Incident;
    nodeName?: string;
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

const statusClassMap: Record<IncidentStatus, string> = {
    Open: styles.open,
    Investigating: styles.investigating,
    Resolved: styles.resolved,
};

export default function IncidentCard({
                                         incident,
                                         nodeName,
                                     }: IncidentCardProps) {
    const affectedNodeName = nodeName ?? incident.computeNodeId;
    const lastActivity = incident.resolvedAt ?? incident.updatedAt;
    const activityLabel = incident.resolvedAt ? "Resolved" : "Updated";

    return (
        <article className={`${styles.card} ${styles[incident.severity.toLowerCase()]}`}>
            <div className={styles.header}>
                <div className={styles.titleGroup}>
                    <h3 className={styles.title}>
                        {incident.title}
                    </h3>

                    <Link
                        href={`/nodes/${incident.computeNodeId}`}
                        className={styles.nodeLink}
                        title={nodeName ? undefined : incident.computeNodeId}
                    >
                        <span className={styles.nodeLabel}>Affected node</span>
                        <span className={styles.nodeName}>{affectedNodeName}</span>
                    </Link>
                </div>

                <span
                    className={`${styles.severity} ${severityClassMap[incident.severity]}`}
                    aria-label={`${incident.severity} severity`}
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
                <span className={`${styles.status} ${statusClassMap[incident.status]}`}>
                    <span className={styles.statusDot} aria-hidden="true" />
                    {incident.status}
                </span>
                <div className={styles.timestamps}>
                    <span>
                        Created{" "}
                        <time dateTime={incident.createdAt}>
                            {new Date(incident.createdAt).toLocaleString()}
                        </time>
                    </span>
                    {lastActivity !== incident.createdAt && (
                        <span>
                            {activityLabel}{" "}
                            <time dateTime={lastActivity}>
                                {new Date(lastActivity).toLocaleString()}
                            </time>
                        </span>
                    )}
                </div>
            </div>
        </article>
    );
}