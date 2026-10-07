import { apiClient } from "./client";
import type {
    Incident,
    IncidentSeverity,
    IncidentStatus,
} from "./models/incident";
import type { PageResponse } from "./models/pageResponse";

type GetIncidentsParams = {
    page?: number;
    pageSize?: number;
    severity?: IncidentSeverity;
    status?: IncidentStatus;
    computeNodeId?: string;
    from?: string;
    to?: string;
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

    search(params: GetIncidentsParams): Promise<PageResponse<Incident>> {
        const searchParams = new URLSearchParams();
        Object.entries(params).forEach(([key, value]) => {
            if (value !== undefined && value !== "") {
                searchParams.set(key, String(value));
            }
        });

        return apiClient.get<PageResponse<Incident>>(
            `/api/v1/incidents/search?${searchParams.toString()}`,
        );
    },

    getById(id: string): Promise<Incident> {
        return apiClient.get<Incident>(
            `/api/v1/incidents/${id}`,
        );
    },
};