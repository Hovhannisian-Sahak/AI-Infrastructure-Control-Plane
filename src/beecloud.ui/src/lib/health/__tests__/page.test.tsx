import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Provider } from "react-redux";

import HealthPage from "../page";
import { nodesApi } from "@/lib/api/nodesApi";
import { createTestStore } from "@/test-utils";

jest.mock("@/lib/api/nodesApi");

const mockedNodesApi = jest.mocked(nodesApi);

const nodes = [
    {
        id: "node-running",
        name: "Running node",
        gpuModel: "NVIDIA H100",
        gpuCount: 4,
        status: "Running" as const,
        activeFault: "None" as const,
    },
    {
        id: "node-stopped",
        name: "Stopped node",
        gpuModel: "NVIDIA A100",
        gpuCount: 2,
        status: "Stopped" as const,
        activeFault: "None" as const,
    },
];

describe("HealthPage", () => {
    beforeEach(() => {
        jest.resetAllMocks();
        mockedNodesApi.getAll.mockResolvedValue(nodes);
        mockedNodesApi.getHealthHistory.mockResolvedValue([]);
    });

    it("requests the selected backend range for every dashboard node", async () => {
        const user = userEvent.setup();
        const store = createTestStore({
            nodes: { nodes },
        });

        render(
            <Provider store={store}>
                <HealthPage />
            </Provider>,
        );

        await waitFor(() => {
            expect(mockedNodesApi.getHealthHistory).toHaveBeenCalledTimes(2);
        });

        for (const node of nodes) {
            expect(mockedNodesApi.getHealthHistory).toHaveBeenCalledWith(
                node.id,
                100,
                expect.any(String),
                expect.any(String),
            );
        }

        const initialRequests =
            mockedNodesApi.getHealthHistory.mock.calls;
        for (const request of initialRequests) {
            expect(Date.parse(request[3]!) - Date.parse(request[2]!))
                .toBe(24 * 60 * 60 * 1000);
        }

        await user.click(
            screen.getByRole("button", { name: "1 hour" }),
        );

        await waitFor(() => {
            expect(mockedNodesApi.getHealthHistory).toHaveBeenCalledTimes(4);
        });

        const selectedRequests =
            mockedNodesApi.getHealthHistory.mock.calls.slice(2);
        expect(selectedRequests.map(request => request[0]).sort()).toEqual([
            "node-running",
            "node-stopped",
        ]);
        for (const request of selectedRequests) {
            expect(request[1]).toBe(100);
            expect(Date.parse(request[3]!) - Date.parse(request[2]!))
                .toBe(60 * 60 * 1000);
        }
    });
});
