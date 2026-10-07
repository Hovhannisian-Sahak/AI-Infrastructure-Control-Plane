"use client";

import { useState } from "react";
import Link from "next/link";

import Pagination from "@/components/common/Pagination";
import {
    formatAlertAge,
    type DerivedHealthAlerts,
    type HealthAlert,
} from "@/lib/health/healthAlerts";

import styles from "./HealthAlerts.module.css";

type HealthAlertsProps = {
    alerts: DerivedHealthAlerts;
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
                   }: {
    alerts: HealthAlert[];
    historical?: boolean;
}) {
    const [page, setPage] = useState(1);
    const pageSize = 10;
    const pageCount = Math.ceil(alerts.length / pageSize);
    const currentPage = Math.min(page, Math.max(1, pageCount));
    const visibleAlerts = historical
        ? alerts.slice((currentPage - 1) * pageSize, currentPage * pageSize)
        : alerts;

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
                {visibleAlerts.map(alert => (
                    <li
                        className={`${styles.alert} ${
                            historical ? styles.historicalAlert : ""
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
                                    ? formatAlertAge(alert.checkedAt)
                                    : "Node currently affected"}
                            </span>
                        </div>
                    </li>
                ))}
            </ul>
            {historical && (
                <Pagination
                    page={currentPage}
                    pageSize={pageSize}
                    totalItems={alerts.length}
                    ariaLabel="Historical health alert pages"
                    onPageChange={setPage}
                />
            )}
        </div>
    );
}

export default function HealthAlerts({
                                         alerts,
                                     }: HealthAlertsProps) {
    const totalAlerts =
        alerts.active.length + alerts.historical.length;

    return (
        <section
            className={styles.panel}
            aria-labelledby="health-alerts-title"
        >
            <div className={styles.header}>
                <div>
                    <p className={styles.eyebrow}>Monitoring</p>
                    <h2 id="health-alerts-title">Health Alerts</h2>
                </div>
                <span className={styles.count} aria-label={`${totalAlerts} alerts`}>
                    {totalAlerts}
                </span>
            </div>

            <section className={styles.group} aria-labelledby="active-alerts-title">
                <h3 id="active-alerts-title">Active</h3>
                <AlertList alerts={alerts.active} />
            </section>

            <section className={styles.group} aria-labelledby="historical-alerts-title">
                <h3 id="historical-alerts-title">History</h3>
                <AlertList alerts={alerts.historical} historical />
            </section>
        </section>
    );
}
