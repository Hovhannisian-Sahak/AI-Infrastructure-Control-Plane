"use client";

import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
    clearIncidentError,
    fetchIncidents,
} from "@/store/slices/incidentsSlice";
import IncidentCard from "./IncidentCard";
import Pagination from "@/components/common/Pagination";
import styles from "./IncidentsSection.module.css";

type SeverityFilter =
    | "All"
    | "Low"
    | "Medium"
    | "High"
    | "Critical";

type StatusFilter = "All" | "Open" | "Investigating" | "Resolved";

export default function IncidentsSection() {
    const dispatch = useAppDispatch();

    const {
        incidents,
        loading,
        error,
        totalCount,
    } = useAppSelector(
        (state) => state.incidents,
    );

    const nodes = useAppSelector(
        (state) => state.nodes.nodes,
    );

    const [severityFilter, setSeverityFilter] =
        useState<SeverityFilter>("All");

    const [statusFilter, setStatusFilter] =
        useState<StatusFilter>("All");

    const [nodeFilter, setNodeFilter] =
        useState("All");

    const [fromDate, setFromDate] =
        useState("");

    const [toDate, setToDate] =
        useState("");

    const [page, setPage] = useState(1);
    const pageSize = 12;

    useEffect(() => {
        dispatch(fetchIncidents({
            page,
            pageSize,
            ...(severityFilter !== "All" && { severity: severityFilter }),
            ...(statusFilter !== "All" && { status: statusFilter }),
            ...(nodeFilter !== "All" && { computeNodeId: nodeFilter }),
            ...(fromDate && { from: `${fromDate}T00:00:00.000Z` }),
            ...(toDate && { to: `${toDate}T23:59:59.999Z` }),
        }));
    }, [
        dispatch,
        page,
        severityFilter,
        statusFilter,
        nodeFilter,
        fromDate,
        toDate,
    ]);

    useEffect(() => {
        if (!error) {
            return;
        }

        const timeoutId = setTimeout(() => {
            dispatch(clearIncidentError());
        }, 5000);

        return () => {
            clearTimeout(timeoutId);
        };
    }, [error, dispatch]);

    const resultCount = totalCount || incidents.length;

    const hasActiveFilters =
        severityFilter !== "All" ||
        statusFilter !== "All" ||
        nodeFilter !== "All" ||
        fromDate !== "" ||
        toDate !== "";

    const handleClearFilters = () => {
        setSeverityFilter("All");
        setStatusFilter("All");
        setNodeFilter("All");
        setFromDate("");
        setToDate("");
        setPage(1);
    };

    return (
        <section className={styles.section}>
            <div className={styles.header}>
                <div>
                    <p className={styles.eyebrow}>
                        Monitoring
                    </p>

                    <h2 className={styles.title}>
                        Incidents
                    </h2>

                    <p className={styles.subtitle}>
                        Monitor incidents reported by the
                        BeeCloud fleet.
                    </p>
                </div>

                <span className={styles.count}>
                    {resultCount}{" "}
                    {resultCount === 1
                        ? "incident"
                        : "incidents"}
                </span>
            </div>

            <div className={styles.filters}>
                    <div className={styles.filterGroup}>
                        <label
                            htmlFor="incident-severity"
                            className={styles.filterLabel}
                        >
                            Severity
                        </label>

                        <select
                            id="incident-severity"
                            className={styles.select}
                            value={severityFilter}
                            onChange={(event) =>
                                {
                                    setSeverityFilter(event.target.value as SeverityFilter);
                                    setPage(1);
                                }
                            }
                        >
                            <option value="All">
                                All severities
                            </option>

                            <option value="Low">
                                Low
                            </option>

                            <option value="Medium">
                                Medium
                            </option>

                            <option value="High">
                                High
                            </option>

                            <option value="Critical">
                                Critical
                            </option>
                        </select>
                    </div>

                    <div className={styles.filterGroup}>
                        <label
                            htmlFor="incident-status"
                            className={styles.filterLabel}
                        >
                            Status
                        </label>

                        <select
                            id="incident-status"
                            className={styles.select}
                            value={statusFilter}
                            onChange={(event) =>
                                {
                                    setStatusFilter(event.target.value as StatusFilter);
                                    setPage(1);
                                }
                            }
                        >
                            <option value="All">
                                All statuses
                            </option>

                            <option value="Open">
                                Open
                            </option>

                            <option value="Investigating">
                                Investigating
                            </option>

                            <option value="Resolved">
                                Resolved
                            </option>
                        </select>
                    </div>

                    <div className={styles.filterGroup}>
                        <label
                            htmlFor="incident-node"
                            className={styles.filterLabel}
                        >
                            Node
                        </label>

                        <select
                            id="incident-node"
                            className={styles.select}
                            value={nodeFilter}
                            onChange={(event) =>
                                {
                                    setNodeFilter(event.target.value);
                                    setPage(1);
                                }
                            }
                        >
                            <option value="All">
                                All nodes
                            </option>

                            {nodes.map((node) => (
                                <option
                                    key={node.id}
                                    value={node.id}
                                >
                                    {node.name}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className={styles.filterGroup}>
                        <label
                            htmlFor="incident-from-date"
                            className={styles.filterLabel}
                        >
                            From
                        </label>

                        <input
                            id="incident-from-date"
                            className={styles.dateInput}
                            type="date"
                            value={fromDate}
                            max={toDate || undefined}
                            onChange={(event) =>
                                {
                                    setFromDate(event.target.value);
                                    setPage(1);
                                }
                            }
                        />
                    </div>

                    <div className={styles.filterGroup}>
                        <label
                            htmlFor="incident-to-date"
                            className={styles.filterLabel}
                        >
                            To
                        </label>

                        <input
                            id="incident-to-date"
                            className={styles.dateInput}
                            type="date"
                            value={toDate}
                            min={fromDate || undefined}
                            onChange={(event) =>
                                {
                                    setToDate(event.target.value);
                                    setPage(1);
                                }
                            }
                        />
                    </div>

                    {hasActiveFilters && (
                            <button
                                type="button"
                                className={styles.clearButton}
                                onClick={handleClearFilters}
                            >
                                Clear filters
                            </button>
                        )}
            </div>

            {loading && incidents.length === 0 && (
                <div className={styles.loading}>
                    <span
                        className={styles.spinner}
                        aria-hidden="true"
                    />
                    Loading incidents...
                </div>
            )}

            {error && (
                <div
                    className={styles.error}
                    role="alert"
                >
                    {error}
                </div>
            )}

            {!loading &&
                !error &&
                resultCount === 0 &&
                hasActiveFilters && (
                    <div className={styles.empty}>
                        <div
                            className={styles.emptyIcon}
                        >
                            !
                        </div>

                        <h3>
                            No matching incidents
                        </h3>

                        <p>
                            No incidents match the
                            selected filters.
                        </p>

                        <button
                            type="button"
                            className={styles.emptyButton}
                            onClick={handleClearFilters}
                        >
                            Clear filters
                        </button>
                    </div>
                )}

            {!loading &&
                !error &&
                resultCount === 0 &&
                !hasActiveFilters && (
                    <div className={styles.empty}>
                        <div
                            className={styles.emptyIcon}
                        >
                            ✓
                        </div>

                        <h3>No incidents</h3>

                        <p>
                            The fleet currently has no
                            reported incidents.
                        </p>
                    </div>
                )}

            {incidents.length > 0 && (
                    <div className={styles.grid} aria-busy={loading}>
                        {incidents.map(
                            (incident) => (
                                <IncidentCard
                                    key={incident.id}
                                    incident={incident}
                                    nodeName={nodes.find(
                                        (node) => node.id === incident.computeNodeId,
                                    )?.name}
                                />
                            ),
                        )}
                    </div>
                )}
            {resultCount > 0 && (
                <Pagination
                    page={page}
                    pageSize={pageSize}
                    totalItems={resultCount}
                    ariaLabel="Incident pages"
                    onPageChange={setPage}
                    disabled={loading}
                    waitForLoading
                />
            )}
        </section>
    );
}