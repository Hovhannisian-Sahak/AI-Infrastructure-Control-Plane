"use client";

import { useEffect } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { fetchNodes } from "@/store/slices/nodesSlice";
import NodeCard from "@/components/nodes/NodeCard";
import styles from "./page.module.css";
import CreateNodeForm from "@/components/nodes/CreateNodeForm";
import { createNode } from "@/store/slices/nodesSlice";
export default function Home() {
    const dispatch = useAppDispatch();

    const {
        nodes,
        loading,
        creating,
        createSuccess,
        error,
    } = useAppSelector((state) => state.nodes);

    useEffect(() => {
        dispatch(fetchNodes());
    }, [dispatch]);

    const handleCreateNode = async (request: {
        name: string;
        gpuModel: string;
        gpuCount: number;
    }) => {
        await dispatch(createNode(request));
    };
    
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

                    <div className={styles.headerActions}>
                        <button
                            className={styles.refreshButton}
                            type="button"
                            onClick={() => dispatch(fetchNodes())}
                            disabled={loading}
                        >
                            {loading ? "Refreshing..." : "Refresh"}
                        </button>

                        <div className={styles.nodeCount}>
                            {nodes.length} {nodes.length === 1 ? "node" : "nodes"}
                        </div>
                    </div>
                </header>

                <CreateNodeForm
                    onSubmit={handleCreateNode}
                    isSubmitting={creating}
                />
                {createSuccess && (
                    <p className={styles.success} role="status">
                        {createSuccess}
                    </p>
                )}
                {loading && (
                    <div className={styles.loadingState}>
                        <span
                            className={styles.spinner}
                            aria-hidden="true"
                        />

                        <p className={styles.loadingMessage}>
                            Loading nodes...
                        </p>
                    </div>
                )}

                {error && (
                    <p className={styles.error} role="alert">
                        {error}
                    </p>
                )}

                {!loading && !error && nodes.length === 0 && (
                    <div className={styles.emptyState}>
                        <h2 className={styles.emptyTitle}>
                            No compute nodes
                        </h2>

                        <p className={styles.emptyMessage}>
                            Create a node to start managing your GPU fleet.
                        </p>
                    </div>
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