import type { ComputeNode } from "@/lib/api/models/computeNode";

type NodeCardProps = {
    node: ComputeNode;
};

export default function NodeCard({ node }: NodeCardProps) {
    return (
        <article>
            <h2>{node.name}</h2>

            <p>
                GPU: {node.gpuModel} × {node.gpuCount}
            </p>

            <p>Status: {node.status}</p>

            <p>Fault: {node.activeFault}</p>
        </article>
    );
}