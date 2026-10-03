"use client";

import { useEffect } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
    createNetwork,
    fetchNetworks,
} from "@/store/slices/networksSlice";
import NetworkCard from "./NetworkCard";
import CreateNetworkForm from "./NetworkForm";

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
        <section>
            <header>
                <h2>Networks</h2>

                <span>
                    {networks.length}{" "}
                    {networks.length === 1
                        ? "network"
                        : "networks"}
                </span>
            </header>

            <CreateNetworkForm
                onSubmit={handleCreateNetwork}
                isSubmitting={creating}
            />

            {createSuccess && (
                <p role="status">
                    {createSuccess}
                </p>
            )}

            {loading && (
                <p>Loading networks...</p>
            )}

            {error && (
                <p role="alert">
                    {error}
                </p>
            )}

            {!loading &&
                !error &&
                networks.length === 0 && (
                    <div>
                        <h3>No networks</h3>
                        <p>
                            Create a network to connect
                            compute nodes.
                        </p>
                    </div>
                )}

            {!loading &&
                !error &&
                networks.length > 0 && (
                    <div>
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