"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";

import Pagination from "@/components/common/Pagination";
import {
    formatAlertAge,
    type DerivedHealthAlerts,
    type HealthAlert,
} from "@/lib/health/healthAlerts";

import styles from "./HealthAlerts.module.css";

const ACTIVE_ALERT_RETENTION_MS = 5 * 60_000;

type HealthAlertsProps = {
    alerts: DerivedHealthAlerts;
};

type TrackedAlert = {
    alert: HealthAlert;
    lastActiveAt: number;
    clearedAt: number | null;
};

function getAlertLabel(type: HealthAlert["type"]) {
    switch (type) {
        case "unhealthy":
            return "Unhealthy";
        case "high-cpu":
            return "High CPU";
        case "high-gpu":
            return "High GPU";
        case "high-temperature":
            return "High GPU temperature";
    }
}

function AlertList({
    alerts,
    historical = false,
    recentlyClearedIds,
}: {
    alerts: HealthAlert[];
    historical?: boolean;
    recentlyClearedIds?: ReadonlySet<string>;
}) {
    const [page, setPage] = useState(1);

    const pageSize = historical ? 10 : 5;
    const pageCount = Math.ceil(alerts.length / pageSize);

    const currentPage = Math.min(
        page,
        Math.max(1, pageCount),
    );

    const visibleAlerts = alerts.slice(
        (currentPage - 1) * pageSize,
        currentPage * pageSize,
    );

    useEffect(() => {
        if (page !== currentPage) {
            setPage(currentPage);
        }
    }, [page, currentPage]);

    useEffect(() => {
        setPage(1);
    }, [alerts.length]);

    if (alerts.length === 0) {
        return (
            <p className={styles.empty} role="status">
                {historical
                    ? "No historical alerts in this range."
                    : "No active health alerts."}
            </p>
        );
    }

    return (
        <div>
            <ul className={styles.list}>
                {visibleAlerts.map(alert => {
                    const recentlyCleared =
                        recentlyClearedIds?.has(alert.id) ?? false;

                    return (
                        <li
                            className={`${styles.alert} ${
    historical
        ? styles.historicalAlert
        : ""
}`}
                            key={alert.id}
                        >
                            <span
                                className={`${styles.indicator} ${
    historical
        ? styles.historicalIndicator
        : styles[alert.type]
}`}
                                aria-hidden="true"
                            />

                            <div className={styles.content}>
                                <div className={styles.alertHeader}>
                                    <Link
                                        href={`/nodes/${alert.nodeId}`}
                                        className={styles.nodeLink}
                                    >
                                        {alert.nodeName}
                                    </Link>

                                    <span className={styles.type}>
                                        {getAlertLabel(alert.type)}
                                    </span>
                                </div>

                                <p>{alert.message}</p>

                                <span className={styles.context}>
                                    {historical
                                        ? formatAlertAge(
                                              alert.checkedAt,
                                          )
                                        : recentlyCleared
                                          ? "Cleared within the last 5 minutes"
                                          : "Node currently affected"}
                                </span>
                            </div>
                        </li>
                    );
                })}
            </ul>

            {pageCount > 1 && (
                <Pagination
                    page={currentPage}
                    pageSize={pageSize}
                    totalItems={alerts.length}
                    ariaLabel={
                        historical
                            ? "Historical health alert pages"
                            : "Active health alert pages"
                    }
                    itemLabel="alerts"
                    onPageChange={setPage}
                />
            )}
        </div>
    );
}

export default function HealthAlerts({
    alerts,
}: HealthAlertsProps) {
    const trackedAlertsRef = useRef(
        new Map<string, TrackedAlert>(),
    );

    const [displayedActiveAlerts, setDisplayedActiveAlerts] =
        useState<TrackedAlert[]>([]);

    const buildDisplayedAlerts = useCallback(
        (now: number) => {
            const trackedAlerts = trackedAlertsRef.current;

            for (const [key, tracked] of trackedAlerts) {
                if (
                    tracked.clearedAt !== null &&
                    now - tracked.clearedAt >=
                        ACTIVE_ALERT_RETENTION_MS
                ) {
                    trackedAlerts.delete(key);
                }
            }

            return [...trackedAlerts.values()].sort(
                (a, b) => {
                    const aCleared =
                        a.clearedAt !== null;

                    const bCleared =
                        b.clearedAt !== null;

                    if (aCleared !== bCleared) {
                        return aCleared ? 1 : -1;
                    }

                    return (
                        b.lastActiveAt -
                        a.lastActiveAt
                    );
                },
            );
        },
        [],
    );

    useEffect(() => {
        const now = Date.now();
        const trackedAlerts = trackedAlertsRef.current;

        const currentKeys = new Set<string>();

        /*
         * 1. Add/update currently active alerts.
         */
        for (const alert of alerts.active) {
            const key = `${alert.nodeId}:${alert.type}`;

            currentKeys.add(key);

            const existing =
                trackedAlerts.get(key);

            trackedAlerts.set(key, {
                alert,
                lastActiveAt:
                    existing?.lastActiveAt ?? now,
                clearedAt: null,
            });
        }

        /*
         * 2. Anything that disappeared from alerts.active
         *    becomes recently cleared.
         */
        for (const [key, tracked] of trackedAlerts) {
            if (
                !currentKeys.has(key) &&
                tracked.clearedAt === null
            ) {
                tracked.clearedAt = now;
            }
        }

        /*
         * 3. Immediately publish the new tracked state.
         */
        setDisplayedActiveAlerts(
            buildDisplayedAlerts(now),
        );
    }, [
        alerts.active,
        buildDisplayedAlerts,
    ]);

    const hasRecentlyClearedAlerts =
        displayedActiveAlerts.some(
            alert =>
                alert.clearedAt !== null,
        );

    /*
     * Re-check every second only while we have
     * recently-cleared alerts.
     */
    useEffect(() => {
        if (!hasRecentlyClearedAlerts) {
            return;
        }

        const intervalId = window.setInterval(() => {
            const now = Date.now();

            setDisplayedActiveAlerts(
                buildDisplayedAlerts(now),
            );
        }, 1000);

        return () => {
            window.clearInterval(intervalId);
        };
    }, [
        hasRecentlyClearedAlerts,
        buildDisplayedAlerts,
    ]);

    const activeAlerts =
        displayedActiveAlerts.map(
            item => item.alert,
        );

    const recentlyClearedIds =
        new Set(
            displayedActiveAlerts
                .filter(
                    item =>
                        item.clearedAt !== null,
                )
                .map(
                    item =>
                        item.alert.id,
                ),
        );

    const totalAlerts =
        activeAlerts.length +
        alerts.historical.length;

    return (
        <section
            className={styles.panel}
            aria-labelledby="health-alerts-title"
        >
            <div className={styles.header}>
                <div>
                    <p className={styles.eyebrow}>
                        Monitoring
                    </p>

                    <h2 id="health-alerts-title">
                        Health Alerts
                    </h2>
                </div>

                <span
                    className={styles.count}
                    aria-label={`${totalAlerts} alerts`}
                >
                    {totalAlerts}
                </span>
            </div>

            <section
                className={styles.group}
                aria-labelledby="active-alerts-title"
            >
                <h3 id="active-alerts-title">
                    Active
                </h3>

                <AlertList
                    alerts={activeAlerts}
                    recentlyClearedIds={
                        recentlyClearedIds
                    }
                />
            </section>

            <section
                className={styles.group}
                aria-labelledby="historical-alerts-title"
            >
                <h3 id="historical-alerts-title">
                    History
                </h3>

                <AlertList
                    alerts={alerts.historical}
                    historical
                />
            </section>
        </section>
    );
}