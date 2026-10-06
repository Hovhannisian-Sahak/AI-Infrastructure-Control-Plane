import { incidentsApi } from "../incidentsApi";
import { apiClient } from "../client";

jest.mock("../client", () => ({
    apiClient: {
        get: jest.fn(),
        post: jest.fn(),
        delete: jest.fn(),
    },
}));


const mockedApiClient = jest.mocked(apiClient);

describe("incidentsApi", () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it("gets all incidents without filters", async () => {
        const incidents = [
            {
                id: "incident-1",
                computeNodeId: "node-1",
                title: "GPU failure",
                description: "GPU failure detected.",
                severity: "Critical" as const,
                status: "Open" as const,
                createdAt: "2026-10-05T10:00:00Z",
            },
        ];

        mockedApiClient.get.mockResolvedValue(incidents);

        const result = await incidentsApi.getAll();

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/incidents",
        );

        expect(result).toEqual(incidents);
    });

    it("gets incidents filtered by severity", async () => {
        mockedApiClient.get.mockResolvedValue([]);

        await incidentsApi.getAll({
            severity: "High",
        });

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/incidents?severity=High",
        );
    });

    it("gets incidents filtered by status", async () => {
        mockedApiClient.get.mockResolvedValue([]);

        await incidentsApi.getAll({
            status: "Open",
        });

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/incidents?status=Open",
        );
    });

    it("gets incidents filtered by severity and status", async () => {
        mockedApiClient.get.mockResolvedValue([]);

        await incidentsApi.getAll({
            severity: "Critical",
            status: "Open",
        });

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/incidents?severity=Critical&status=Open",
        );
    });

    it("gets an incident by id", async () => {
        const incident = {
            id: "incident-1",
            computeNodeId: "node-1",
            title: "GPU failure",
            description: "GPU failure detected.",
            severity: "Critical" as const,
            status: "Open" as const,
            createdAt: "2026-10-05T10:00:00Z",
        };

        mockedApiClient.get.mockResolvedValue(incident);

        const result = await incidentsApi.getById("incident-1");

        expect(mockedApiClient.get).toHaveBeenCalledWith(
            "/api/v1/incidents/incident-1",
        );

        expect(result).toEqual(incident);
    });
});