"use client";

import type { Incident } from "@/lib/api/models/incident";
import IncidentCard from "./IncidentCard";
import styles from "./IncidentNodeHistory.module.css";

type IncidentNodeHistoryProps = {
    nodeId: string;
    nodeName?: string;
    incidents: Incident[];
};

export default function IncidentNodeHistory({
    nodeId,
    nodeName,
    incidents,
}: IncidentNodeHistoryProps) {
    const label = nodeName ?? nodeId;

    return (
        <details className={styles.group}>
            <summary className={styles.summary}>
                <span className={styles.nodeName}>{label}</span>
                <span className={styles.count}>
                    {incidents.length}{" "}
                    {incidents.length === 1 ? "incident" : "incidents"}
                </span>
            </summary>
            <div className={styles.episodes}>
                {incidents.map((incident) => (
                    <IncidentCard
                        key={incident.id}
                        incident={incident}
                        nodeName={nodeName}
                    />
                ))}
            </div>
        </details>
    );
}
