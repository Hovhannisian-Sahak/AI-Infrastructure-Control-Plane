import Link from "next/link";

import type { HealthAlert as HealthAlertItem } from "@/lib/health/healthAlerts";

import styles from "./HealthAlerts.module.css";

type HealthAlertsProps = {
    alerts: HealthAlertItem[];
};

export default function HealthAlerts({
                                         alerts,
                                     }: HealthAlertsProps) {
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
                <span className={styles.count} aria-label={`${alerts.length} alerts`}>
                    {alerts.length}
                </span>
            </div>

            {alerts.length === 0 ? (
                <p className={styles.empty} role="status">
                    No active health alerts.
                </p>
            ) : (
                <ul className={styles.list}>
                    {alerts.map(alert => (
                        <li className={styles.alert} key={alert.id}>
                            <span
                                className={`${styles.indicator} ${styles[alert.type]}`}
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
                                        {alert.type === "unhealthy"
                                            ? "Unhealthy"
                                            : alert.type === "high-cpu"
                                                ? "High CPU"
                                                : "High GPU"}
                                    </span>
                                </div>
                                <p>{alert.message}</p>
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </section>
    );
}
