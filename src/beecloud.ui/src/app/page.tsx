"use client";

import { useEffect, useState } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
  createNode,
  fetchNodes,
} from "@/store/slices/nodesSlice";
import NodeCard from "@/components/nodes/NodeCard";
import CreateNodeForm from "@/components/nodes/CreateNodeForm";
import NetworksSection from "@/components/networks/NetworksSection";
import IncidentsSection from "@/components/incidents/IncidentsSection";
import { nodesApi } from "@/lib/api/nodesApi";
import type { HealthCheck } from "@/lib/api/models/healthCheck";
import styles from "./page.module.css";

export default function Home() {
  const dispatch = useAppDispatch();

  const {
    nodes,
    loading,
    creating,
    createSuccess,
    error,
  } = useAppSelector((state) => state.nodes);

  const [healthHistoryByNodeId, setHealthHistoryByNodeId] =
      useState<Record<string, HealthCheck[]>>({});

  useEffect(() => {
    dispatch(fetchNodes());
  }, [dispatch]);

  useEffect(() => {
    const hasTransitionalNodes = nodes.some(
        (node) =>
            node.status === "Provisioning" ||
            node.status === "Stopping",
    );

    if (!hasTransitionalNodes) {
      return;
    }

    const intervalId = setInterval(() => {
      dispatch(fetchNodes());
    }, 5000);

    return () => {
      clearInterval(intervalId);
    };
  }, [dispatch, nodes]);

  useEffect(() => {
    let cancelled = false;

    const loadHealthHistory = async () => {
      const monitoredNodes = nodes.filter(
          (node) =>
              node.status === "Running" ||
              node.status === "Unhealthy" ||
              node.status === "Quarantined" ||
              node.status === "Remediating",
      );

      if (monitoredNodes.length === 0) {
        return;
      }

      const results = await Promise.all(
          monitoredNodes.map(async (node) => {
            try {
              const history =
                  await nodesApi.getHealthHistory(
                      node.id,
                      10,
                  );

              const latestHistory = [...history]
                  .sort(
                      (a, b) =>
                          new Date(a.checkedAt).getTime() -
                          new Date(b.checkedAt).getTime(),
                  )
                  .slice(-10);

              return [node.id, latestHistory] as const;
            } catch {
              return null;
            }
          }),
      );

      if (cancelled) {
        return;
      }

      setHealthHistoryByNodeId((current) => {
        const next = { ...current };

        for (const result of results) {
          if (result) {
            const [nodeId, history] = result;
            next[nodeId] = history;
          }
        }

        return next;
      });
    };

    void loadHealthHistory();

    const intervalId = setInterval(() => {
      void loadHealthHistory();
    }, 10_000);

    return () => {
      cancelled = true;
      clearInterval(intervalId);
    };
  }, [nodes]);

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
              <h1 className={styles.title}>
                BeeCloud Nodes
              </h1>

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
                {nodes.length}{" "}
                {nodes.length === 1 ? "node" : "nodes"}
              </div>
            </div>
          </header>

          <CreateNodeForm
              onSubmit={handleCreateNode}
              isSubmitting={creating}
          />

          {createSuccess && (
              <p
                  className={styles.success}
                  role="status"
              >
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
              <p
                  className={styles.error}
                  role="alert"
              >
                {error}
              </p>
          )}

          {!loading && nodes.length === 0 && (
              <div className={styles.emptyState}>
                <h2 className={styles.emptyTitle}>
                  No compute nodes
                </h2>

                <p className={styles.emptyMessage}>
                  Create a node to start managing your GPU
                  fleet.
                </p>
              </div>
          )}

          {!loading && nodes.length > 0 && (
              <section className={styles.nodes}>
                {nodes.map((node) => (
                    <NodeCard
                        key={node.id}
                        node={node}
                        healthHistory={
                            healthHistoryByNodeId[node.id] ?? []
                        }
                    />
                ))}
              </section>
          )}

          <NetworksSection />
          <IncidentsSection />
        </main>
      </div>
  );
}