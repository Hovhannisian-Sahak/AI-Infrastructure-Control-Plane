import { apiClient } from "./client";
import type {
    Incident,
    IncidentSeverity,
    IncidentStatus,
} from "./models/incident";

type GetIncidentsParams = {
    severity?: IncidentSeverity;
    status?: IncidentStatus;
};

export const incidentsApi = {
    getAll(
        params?: GetIncidentsParams,
    ): Promise<Incident[]> {
        const searchParams = new URLSearchParams();

        if (params?.severity) {
            searchParams.set("severity", params.severity);
        }

        if (params?.status) {
            searchParams.set("status", params.status);
        }

        const query = searchParams.toString();

        return apiClient.get<Incident[]>(
            `/api/v1/incidents${query ? `?${query}` : ""}`,
        );
    },

    getById(id: string): Promise<Incident> {
        return apiClient.get<Incident>(
            `/api/v1/incidents/${id}`,
        );
    },
};