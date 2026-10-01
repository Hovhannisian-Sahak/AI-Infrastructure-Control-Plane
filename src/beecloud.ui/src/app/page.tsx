"use client";

import { useEffect } from "react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { fetchNodes } from "@/store/slices/nodesSlice";
import NodeCard from "@/components/nodes/NodeCard";

export default function Home() {
  const dispatch = useAppDispatch();

  const { nodes, loading, error } = useAppSelector(
      (state) => state.nodes,
  );

  useEffect(() => {
    dispatch(fetchNodes());
  }, [dispatch]);

  return (
      <main>
        <h1>BeeCloud Nodes</h1>

        {loading && <p>Loading nodes...</p>}

        {error && <p role="alert">{error}</p>}

        {!loading && !error && nodes.length === 0 && (
            <p>No compute nodes found.</p>
        )}

        {!loading && !error && nodes.length > 0 && (
            <section>
              {nodes.map((node) => (
                  <NodeCard key={node.id} node={node} />
              ))}
            </section>
        )}
      </main>
  );
}