"use client";

import { useState } from "react";
import type { Incident } from "@/lib/api/models/incident";
import Pagination from "@/components/common/Pagination";
import IncidentCard from "./IncidentCard";
import styles from "./IncidentNodeHistory.module.css";

const EPISODES_PER_PAGE = 6;

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
    const [page, setPage] = useState(1);
    const pageCount = Math.ceil(incidents.length / EPISODES_PER_PAGE);
    const currentPage = Math.min(page, pageCount);
    const pageIncidents = incidents.slice(
        (currentPage - 1) * EPISODES_PER_PAGE,
        currentPage * EPISODES_PER_PAGE,
    );

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
                {pageIncidents.map((incident) => (
                    <IncidentCard
                        key={incident.id}
                        incident={incident}
                        nodeName={nodeName}
                    />
                ))}
            </div>
            <div className={styles.pagination}>
                <Pagination
                    page={currentPage}
                    pageSize={EPISODES_PER_PAGE}
                    totalItems={incidents.length}
                    ariaLabel={`Incident history for ${label}`}
                    itemLabel="episodes"
                    onPageChange={setPage}
                />
            </div>
        </details>
    );
}
