"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import styles from "./SiteNavigation.module.css";

export default function SiteNavigation() {
    const pathname = usePathname();
    const nodesActive = pathname === "/" || pathname.startsWith("/nodes/");
    const healthActive = pathname === "/health";

    return (
        <nav className={styles.navigation} aria-label="Main navigation">
            <div className={styles.content}>
                <Link className={styles.brand} href="/">
                    BeeCloud
                </Link>
                <div className={styles.links}>
                    <Link
                        className={nodesActive ? styles.activeLink : styles.link}
                        href="/"
                        aria-current={nodesActive ? "page" : undefined}
                    >
                        Nodes
                    </Link>
                    <Link
                        className={healthActive ? styles.activeLink : styles.link}
                        href="/health"
                        aria-current={healthActive ? "page" : undefined}
                    >
                        Health
                    </Link>
                </div>
            </div>
        </nav>
    );
}
