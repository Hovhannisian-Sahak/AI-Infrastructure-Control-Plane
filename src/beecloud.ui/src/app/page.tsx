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

import type { NodeStatus } from "@/lib/api/models/computeNode";
import styles from "./page.module.css";

const NODE_PAGE_SIZE = 8;
const MIN_REFRESH_FEEDBACK_MS = 350;
const NODE_STATUSES: NodeStatus[] = [
  "Provisioning", "Available", "Running", "Stopping", "Stopped",
  "Unhealthy", "Quarantined", "Remediating", "Failed",
];

export default function Home() {
  const dispatch = useAppDispatch();
  const [nodePage, setNodePage] = useState(1);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<NodeStatus | "all">("all");
  const [gpuModelFilter, setGpuModelFilter] = useState("all");
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
  const gpuModels = Array.from(
      new Set(nodes.map((node) => node.gpuModel)),
  ).sort((a, b) => a.localeCompare(b));

  const normalizedSearch = searchTerm.trim().toLocaleLowerCase();
  const filteredNodes = nodes.filter((node) => {
    const matchesName =
        normalizedSearch.length === 0 ||
        node.name.toLocaleLowerCase().includes(normalizedSearch);
    const matchesStatus =
        statusFilter === "all" || node.status === statusFilter;
    const matchesGpuModel =
        gpuModelFilter === "all" || node.gpuModel === gpuModelFilter;

    return matchesName && matchesStatus && matchesGpuModel;
  });

  const hasActiveFilters =
      normalizedSearch.length > 0 ||
      statusFilter !== "all" ||
      gpuModelFilter !== "all";

  const clearFilters = () => {
    setSearchTerm("");
    setStatusFilter("all");
    setGpuModelFilter("all");
    setNodePage(1);
  };

  const nodePageCount = Math.ceil(filteredNodes.length / NODE_PAGE_SIZE);
  const currentNodePage = Math.min(nodePage, Math.max(1, nodePageCount));
  const visibleNodes = filteredNodes.slice(
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

          {nodes.length > 0 && (
              <section className={styles.filters} aria-label="Filter compute nodes">
                <div className={styles.filterField}>
                  <label className={styles.filterLabel} htmlFor="node-search">Search nodes</label>
                  <input
                      id="node-search"
                      className={styles.filterInput}
                      type="search"
                      placeholder="Search by node name"
                      value={searchTerm}
                      onChange={(event) => {
                        setSearchTerm(event.target.value);
                        setNodePage(1);
                      }}
                  />
                </div>
                <div className={styles.filterField}>
                  <label className={styles.filterLabel} htmlFor="node-status-filter">Filter by status</label>
                  <select
                      id="node-status-filter"
                      className={styles.filterInput}
                      value={statusFilter}
                      onChange={(event) => {
                        setStatusFilter(event.target.value as NodeStatus | "all");
                        setNodePage(1);
                      }}
                  >
                    <option value="all">All statuses</option>
                    {NODE_STATUSES.map((status) => (
                        <option key={status} value={status}>{status}</option>
                    ))}
                  </select>
                </div>
                <div className={styles.filterField}>
                  <label className={styles.filterLabel} htmlFor="node-gpu-filter">Filter by GPU model</label>
                  <select
                      id="node-gpu-filter"
                      className={styles.filterInput}
                      value={gpuModelFilter}
                      onChange={(event) => {
                        setGpuModelFilter(event.target.value);
                        setNodePage(1);
                      }}
                  >
                    <option value="all">All GPU models</option>
                    {gpuModels.map((model) => (
                        <option key={model} value={model}>{model}</option>
                    ))}
                  </select>
                </div>
                {hasActiveFilters && (
                    <button className={styles.clearFiltersButton} type="button" onClick={clearFilters}>
                      Clear filters
                    </button>
                )}
                <p className={styles.filterSummary}>
                  {filteredNodes.length} matching {filteredNodes.length === 1 ? "node" : "nodes"}
                </p>
              </section>
          )}

          {!loading && nodes.length > 0 && filteredNodes.length === 0 && (
              <div className={styles.emptyState}>
                <h2 className={styles.emptyTitle}>No matching nodes</h2>
                <p className={styles.emptyMessage}>Try a different search or clear the active filters.</p>
              </div>
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

          {filteredNodes.length > 0 && (
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
                  totalItems={filteredNodes.length}
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