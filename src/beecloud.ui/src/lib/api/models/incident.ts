export type IncidentSeverity =
    | "Low"
    | "Medium"
    | "High"
    | "Critical";

export type IncidentStatus =
    | "Open"
    | "Investigating"
    | "Resolved";

export type Incident = {
    id: string;
    computeNodeId: string;
    severity: IncidentSeverity;
    status: IncidentStatus;
    title: string;
    description?: string | null;
    createdAt: string;
    updatedAt: string;
    lastSeenAt: string;
    occurrenceCount: number;
    resolvedAt?: string | null;
};