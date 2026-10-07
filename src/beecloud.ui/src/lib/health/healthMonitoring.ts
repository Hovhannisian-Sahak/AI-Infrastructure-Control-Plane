import type { ComputeNode, NodeStatus } from "@/lib/api/models/computeNode";

const MONITORED_STATUSES: readonly NodeStatus[] = [
    "Running",
    "Unhealthy",
    "Quarantined",
    "Remediating",
];

type NodeIdentity = Pick<ComputeNode, "id" | "status">;

export function isHealthMonitored(status: NodeStatus): boolean {
    return MONITORED_STATUSES.includes(status);
}

export function getHealthMonitoringNodeIds(
    nodes: NodeIdentity[],
    monitored: boolean,
): string[] {
    return nodes
        .filter(node => isHealthMonitored(node.status) === monitored)
        .map(node => node.id)
        .sort();
}
