"use client";

import { useEffect } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
  createNetwork,
  fetchNetworks,
} from "@/store/slices/networksSlice";
import NetworkCard from "./NetworkCard";
import CreateNetworkForm from "./CreateNetworkForm";
import styles from "./NetworksSection.module.css";

export default function NetworksSection() {
  const dispatch = useAppDispatch();

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

  const handleCreateNetwork = async (request: {
    name: string;
    description?: string;
  }) => {
    await dispatch(createNetwork(request));
  };

  return (
      <section className={styles.section}>
        <div className={styles.header}>
          <div>
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
              {createSuccess}
            </div>
        )}

        {loading && (
            <div className={styles.loading}>
              <span className={styles.spinner} />
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
                  <div className={styles.emptyIcon}>
                    N
                  </div>

                  <h3>No networks yet</h3>

                  <p>
                    Create your first network to
                    connect compute nodes.
                  </p>
                </div>
            )}

        {!loading &&
            !error &&
            networks.length > 0 && (
                <div className={styles.grid}>
                  {networks.map((network) => (
                      <NetworkCard
                          key={network.id}
                          network={network}
                      />
                  ))}
                </div>
            )}
      </section>
  );
}