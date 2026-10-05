"use client";

import { useEffect } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
    clearIncidentError,
    fetchIncidents,
} from "@/store/slices/incidentsSlice";
import IncidentCard from "./IncidentCard";
import styles from "./IncidentsSection.module.css";

export default function IncidentsSection() {
    const dispatch = useAppDispatch();

    const {
        incidents,
        loading,
        error,
    } = useAppSelector(
        (state) => state.incidents,
    );

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
          {incidents.length}{" "}
                    {incidents.length === 1
                        ? "incident"
                        : "incidents"}
        </span>
            </div>

            {loading && (
                <div className={styles.loading}>
                    <span className={styles.spinner} />
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

            {!loading && incidents.length === 0 && (
                <div className={styles.empty}>
                    <div className={styles.emptyIcon}>
                        ✓
                    </div>

                    <h3>No incidents</h3>

                    <p>
                        The fleet currently has no reported
                        incidents.
                    </p>
                </div>
            )}

            {!loading && incidents.length > 0 && (
                <div className={styles.grid}>
                    {incidents.map((incident) => (
                        <IncidentCard
                            key={incident.id}
                            incident={incident}
                        />
                    ))}
                </div>
            )}
        </section>
    );
}