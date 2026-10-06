"use client";

import { useEffect, useMemo, useState } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
    clearIncidentError,
    fetchIncidents,
} from "@/store/slices/incidentsSlice";
import IncidentCard from "./IncidentCard";
import styles from "./IncidentsSection.module.css";

type SeverityFilter =
    | "All"
    | "Low"
    | "Medium"
    | "High"
    | "Critical";

type StatusFilter = "All" | "Open" | "Resolved";

export default function IncidentsSection() {
    const dispatch = useAppDispatch();

    const {
        incidents,
        loading,
        error,
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

    useEffect(() => {
        dispatch(fetchIncidents());
    }, [dispatch]);

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

    const nodeOptions = useMemo(() => {
        const nodeIds = new Set(
            incidents.map(
                (incident) => incident.computeNodeId,
            ),
        );

        return Array.from(nodeIds).map((nodeId) => {
            const node = nodes.find(
                (item) => item.id === nodeId,
            );

            return {
                id: nodeId,
                name: node?.name ?? nodeId,
            };
        });
    }, [incidents, nodes]);

    const filteredIncidents = useMemo(() => {
        return incidents.filter((incident) => {
            const matchesSeverity =
                severityFilter === "All" ||
                incident.severity === severityFilter;

            const matchesStatus =
                statusFilter === "All" ||
                incident.status === statusFilter;

            const matchesNode =
                nodeFilter === "All" ||
                incident.computeNodeId === nodeFilter;

            const incidentDate = new Date(
                incident.createdAt,
            );

            const matchesFromDate =
                !fromDate ||
                incidentDate >=
                new Date(`${fromDate}T00:00:00`);

            const matchesToDate =
                !toDate ||
                incidentDate <=
                new Date(`${toDate}T23:59:59.999`);

            return (
                matchesSeverity &&
                matchesStatus &&
                matchesNode &&
                matchesFromDate &&
                matchesToDate
            );
        });
    }, [
        incidents,
        severityFilter,
        statusFilter,
        nodeFilter,
        fromDate,
        toDate,
    ]);

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
                    {filteredIncidents.length}{" "}
                    {filteredIncidents.length === 1
                        ? "incident"
                        : "incidents"}
                </span>
            </div>

            {!loading && incidents.length > 0 && (
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
                                setSeverityFilter(
                                    event.target.value as SeverityFilter,
                                )
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
                                setStatusFilter(
                                    event.target.value as StatusFilter,
                                )
                            }
                        >
                            <option value="All">
                                All statuses
                            </option>

                            <option value="Open">
                                Open
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
                                setNodeFilter(
                                    event.target.value,
                                )
                            }
                        >
                            <option value="All">
                                All nodes
                            </option>

                            {nodeOptions.map((node) => (
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
                                setFromDate(
                                    event.target.value,
                                )
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
                                setToDate(
                                    event.target.value,
                                )
                            }
                        />
                    </div>

                    {hasActiveFilters &&
                        filteredIncidents.length > 0 && (
                            <button
                                type="button"
                                className={styles.clearButton}
                                onClick={handleClearFilters}
                            >
                                Clear filters
                            </button>
                        )}
                </div>
            )}

            {loading && (
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
                incidents.length > 0 &&
                filteredIncidents.length === 0 && (
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
                incidents.length === 0 && (
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

            {!loading &&
                filteredIncidents.length > 0 && (
                    <div className={styles.grid}>
                        {filteredIncidents.map(
                            (incident) => (
                                <IncidentCard
                                    key={incident.id}
                                    incident={incident}
                                />
                            ),
                        )}
                    </div>
                )}
        </section>
    );
}