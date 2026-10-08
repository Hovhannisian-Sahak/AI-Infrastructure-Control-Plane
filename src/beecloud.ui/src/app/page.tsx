"use client";

import { useEffect, useState } from "react";

import {
  useAppDispatch,
  useAppSelector,
} from "@/store/hooks";

import {
  fetchNodes,
  createNode,
} from "@/store/slices/nodesSlice";

import {
  fetchHealthHistory,
} from "@/store/slices/healthSlice";
import { fetchNodeMetrics } from "@/store/slices/metricsSlice";
import { getHealthMonitoringNodeIds } from "@/lib/health/healthMonitoring";

import NodeCard from "@/components/nodes/NodeCard";
import CreateNodeForm from "@/components/nodes/CreateNodeForm";
import NetworksSection from "@/components/networks/NetworksSection";
import IncidentsSection from "@/components/incidents/IncidentsSection";
import Pagination from "@/components/common/Pagination";

import styles from "./page.module.css";

const NODE_PAGE_SIZE = 8;
const MIN_REFRESH_FEEDBACK_MS = 350;

export default function Home() {
  const dispatch = useAppDispatch();
  const [nodePage, setNodePage] = useState(1);
  const [manualRefreshPending, setManualRefreshPending] = useState(false);

  const {
    nodes,
    loading,
    refreshing,
    creating,
    createSuccess,
    error,
  } = useAppSelector(
      (state) => state.nodes,
  );

  useEffect(() => {
    dispatch(fetchNodes());
  }, [dispatch]);

  useEffect(() => {
    if (nodes.length === 0) {
      return;
    }

    const hasTransitionalNodes =
        nodes.some(
            (node) =>
                node.status === "Provisioning" ||
                node.status === "Stopping",
        );

    const intervalId = setInterval(() => {
      dispatch(fetchNodes());
    }, hasTransitionalNodes ? 5000 : 10_000);

    return () => {
      clearInterval(intervalId);
    };
  }, [dispatch, nodes]);

  /*
   * Health polling is intentionally independent
   * from the node object reference.
   *
   * We only depend on the IDs of nodes whose
   * health should currently be monitored.
   */
  const monitoredNodeIds =
      getHealthMonitoringNodeIds(nodes, true);
  const unmonitoredNodeIds =
      getHealthMonitoringNodeIds(nodes, false);

  const monitoredNodeKey = monitoredNodeIds.join(",");
  const unmonitoredNodeKey = unmonitoredNodeIds.join(",");

  useEffect(() => {
    if (!unmonitoredNodeKey) {
      return;
    }

    for (const nodeId of unmonitoredNodeKey.split(",")) {
      void dispatch(
          fetchHealthHistory({
            nodeId,
            limit: 10,
          }),
      );
      void dispatch(fetchNodeMetrics({
        nodeId,
        limit: 10,
      }));
    }
  }, [dispatch, unmonitoredNodeKey]);

  useEffect(() => {
    if (!monitoredNodeKey) {
      return;
    }

    const currentMonitoredNodeIds =
        monitoredNodeKey.split(",");

    const loadHealth = () => {
      for (const nodeId of currentMonitoredNodeIds) {
        void dispatch(
            fetchHealthHistory({
              nodeId,
              limit: 10,
            }),
        );
        void dispatch(fetchNodeMetrics({
          nodeId,
          limit: 10,
        }));
      }
    };

    loadHealth();

    const intervalId = setInterval(
        loadHealth,
        10_000,
    );

    return () => {
      clearInterval(intervalId);
    };
  }, [
    dispatch,
    monitoredNodeKey,
  ]);

  const handleCreateNode = async (
      request: {
        name: string;
        gpuModel: string;
        gpuCount: number;
      },
  ) => {
    const result = await dispatch(
        createNode(request),
    );
    if (createNode.fulfilled.match(result)) {
      setNodePage(1);
    }
  };
  const handleRefreshNodes = async () => {
    const startedAt = Date.now();
    setManualRefreshPending(true);

    try {
      await dispatch(fetchNodes());
    } finally {
      const remainingFeedbackTime =
          MIN_REFRESH_FEEDBACK_MS - (Date.now() - startedAt);
      if (remainingFeedbackTime > 0) {
        await new Promise(resolve =>
            window.setTimeout(resolve, remainingFeedbackTime),
        );
      }

      setManualRefreshPending(false);
    }
  };
  const nodePageCount = Math.ceil(nodes.length / NODE_PAGE_SIZE);
  const currentNodePage = Math.min(nodePage, Math.max(1, nodePageCount));
  const visibleNodes = nodes.slice(
      (currentNodePage - 1) * NODE_PAGE_SIZE,
      currentNodePage * NODE_PAGE_SIZE,
  );

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
                  onClick={handleRefreshNodes}
                  disabled={loading || refreshing || manualRefreshPending}
              >
                {loading || refreshing || manualRefreshPending
                    ? "Refreshing..."
                    : "Refresh"}
              </button>

              <div className={styles.nodeCount}>
                {nodes.length}{" "}
                {nodes.length === 1
                    ? "node"
                    : "nodes"}
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
              <div
                  className={styles.loadingState}
              >
            <span
                className={styles.spinner}
                aria-hidden="true"
            />

                <p
                    className={styles.loadingMessage}
                >
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

          {!loading &&
              nodes.length === 0 && (
                  <div
                      className={styles.emptyState}
                  >
                    <h2
                        className={styles.emptyTitle}
                    >
                      No compute nodes
                    </h2>

                    <p
                        className={
                          styles.emptyMessage
                        }
                    >
                      Create a node to start
                      managing your GPU fleet.
                    </p>
                  </div>
              )}

          {nodes.length > 0 && (
                  <section
                      className={styles.nodes}
                  >
                    {visibleNodes.map((node) => (
                        <NodeCard
                            key={node.id}
                            node={node}
                        />
                    ))}
                  </section>
              )}
          {nodes.length > 0 && (
              <Pagination
                  page={currentNodePage}
                  pageSize={NODE_PAGE_SIZE}
                  totalItems={nodes.length}
                  ariaLabel="Compute node pages"
                  itemLabel="nodes"
                  onPageChange={setNodePage}
                  disabled={loading}
              />
          )}

          <NetworksSection />
          <IncidentsSection />
        </main>
      </div>
  );
}