"use client";

import { useEffect, useState } from "react";
import {
  useAppDispatch,
  useAppSelector,
} from "@/store/hooks";
import {
  clearNetworkError,
  createNetwork,
  fetchNetworks,
} from "@/store/slices/networksSlice";
import NetworkCard from "./NetworkCard";
import CreateNetworkForm from "./CreateNetworkForm";
import Pagination from "@/components/common/Pagination";
import styles from "./NetworksSection.module.css";

const NETWORK_PAGE_SIZE = 6;

export default function NetworksSection() {
  const dispatch = useAppDispatch();
  const [networkPage, setNetworkPage] = useState(1);

  const {
    networks,
    loading,
    creating,
    error,
    createSuccess,
  } = useAppSelector(
      (state) => state.networks,
  );

  useEffect(() => {
    dispatch(fetchNetworks());
  }, [dispatch]);

  useEffect(() => {
    if (!error) {
      return;
    }

    const timeoutId = setTimeout(() => {
      dispatch(clearNetworkError());
    }, 5000);

    return () => {
      clearTimeout(timeoutId);
    };
  }, [error, dispatch]);

  const handleCreateNetwork = async (
      request: {
        name: string;
        description?: string;
      },
  ) => {
    await dispatch(createNetwork(request));
  };
  const pageCount = Math.ceil(networks.length / NETWORK_PAGE_SIZE);
  const currentPage = Math.min(networkPage, Math.max(1, pageCount));
  const visibleNetworks = networks.slice(
      (currentPage - 1) * NETWORK_PAGE_SIZE,
      currentPage * NETWORK_PAGE_SIZE,
  );

  return (
      <section className={styles.section}>
        <div className={styles.header}>
          <div className={styles.heading}>
            <p className={styles.eyebrow}>
              Infrastructure
            </p>

            <h2 className={styles.title}>
              Networks
            </h2>

            <p className={styles.subtitle}>
              Manage network connectivity between
              compute nodes.
            </p>
          </div>

          <span className={styles.count}>
          {networks.length}{" "}
            {networks.length === 1
                ? "network"
                : "networks"}
        </span>
        </div>

        <CreateNetworkForm
            onSubmit={handleCreateNetwork}
            isSubmitting={creating}
        />

        {createSuccess && (
            <div
                className={styles.success}
                role="status"
            >
          <span
              className={styles.successIcon}
              aria-hidden="true"
          >
            ✓
          </span>

              {createSuccess}
            </div>
        )}

        {loading && (
            <div className={styles.loading}>
          <span
              className={styles.spinner}
              aria-hidden="true"
          />

              Loading networks...
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

        {!loading &&
            !error &&
            networks.length === 0 && (
                <div className={styles.empty}>
                  <div
                      className={styles.emptyIcon}
                      aria-hidden="true"
                  >
                    N
                  </div>

                  <h3>No networks yet</h3>

                  <p>
                    Create your first network to
                    connect compute nodes.
                  </p>
                </div>
            )}

        {networks.length > 0 && (
                <div className={styles.grid}>
                  {visibleNetworks.map((network) => (
                      <NetworkCard
                          key={network.id}
                          network={network}
                      />
                  ))}
                </div>
            )}
        {networks.length > 0 && (
            <Pagination
                page={currentPage}
                pageSize={NETWORK_PAGE_SIZE}
                totalItems={networks.length}
                ariaLabel="Network pages"
                onPageChange={setNetworkPage}
                disabled={loading}
            />
        )}
      </section>
  );
}