"use client";

import { useEffect } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { fetchNodes } from "@/store/slices/nodesSlice";
import NodeCard from "@/components/nodes/NodeCard";
import styles from "./page.module.css";

export default function Home() {
    const dispatch = useAppDispatch();

    const { nodes, loading, error } = useAppSelector(
        (state) => state.nodes,
    );

    useEffect(() => {
        dispatch(fetchNodes());
    }, [dispatch]);

    return (
        <div className={styles.page}>
            <main className={styles.main}>
                <header className={styles.header}>
                    <div>
                        <h1 className={styles.title}>BeeCloud Nodes</h1>
                        <p className={styles.subtitle}>
                            Monitor and manage compute nodes.
                        </p>
                    </div>

                    <div className={styles.nodeCount}>
                        {nodes.length} nodes
                    </div>
                </header>

                {loading && (
                    <p className={styles.message}>Loading nodes...</p>
                )}

                {error && (
                    <p className={styles.error} role="alert">
                        {error}
                    </p>
                )}

                {!loading && !error && nodes.length === 0 && (
                    <p className={styles.message}>
                        No compute nodes found.
                    </p>
                )}

                {!loading && !error && nodes.length > 0 && (
                    <section className={styles.nodes}>
                        {nodes.map((node) => (
                            <NodeCard key={node.id} node={node} />
                        ))}
                    </section>
                )}
            </main>
        </div>
    );
}