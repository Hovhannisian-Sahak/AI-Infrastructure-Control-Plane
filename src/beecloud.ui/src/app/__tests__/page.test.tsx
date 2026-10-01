import { screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "@/test-utils";
import Home from "../page";
import { nodesApi } from "@/lib/api/nodesApi";

jest.mock("@/lib/api/nodesApi");

const mockedNodesApi = jest.mocked(nodesApi);

describe("Home page", () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it("renders the page heading", async () => {
        mockedNodesApi.getAll.mockResolvedValue([]);

        renderWithProviders(<Home />);

        expect(
            screen.getByRole("heading", { name: "BeeCloud Nodes" }),
        ).toBeInTheDocument();

        await waitFor(() => {
            expect(mockedNodesApi.getAll).toHaveBeenCalledTimes(1);
        });
    });

    it("renders nodes returned by the API", async () => {
        mockedNodesApi.getAll.mockResolvedValue([
            {
                id: "node-1",
                name: "GPU Node 1",
                gpuModel: "NVIDIA A100",
                gpuCount: 4,
                status: "Available",
                activeFault: "None",
            },
        ]);

        renderWithProviders(<Home />);

        expect(await screen.findByRole("heading", {
            name: "GPU Node 1",
        })).toBeInTheDocument();

        expect(
            screen.getByText("GPU: NVIDIA A100 × 4"),
        ).toBeInTheDocument();

        expect(
            screen.getByText("Status: Available"),
        ).toBeInTheDocument();
    });

    it("renders an error when the API request fails", async () => {
        mockedNodesApi.getAll.mockRejectedValue(
            new Error("API unavailable"),
        );

        renderWithProviders(<Home />);

        expect(
            await screen.findByRole("alert"),
        ).toHaveTextContent("API unavailable");
    });
});