import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test-utils";
import { nodesApi } from "@/lib/api/nodesApi";
import { networksApi } from "@/lib/api/networksApi";
import { incidentsApi } from "@/lib/api/incidentsApi";
import type { ComputeNode } from "@/lib/api/models/computeNode";
import Home from "../page";

jest.mock("@/lib/api/nodesApi");
jest.mock("@/lib/api/networksApi");
jest.mock("@/lib/api/incidentsApi");

const mockedNodesApi = jest.mocked(nodesApi);
const mockedNetworksApi = jest.mocked(networksApi);
const mockedIncidentsApi = jest.mocked(incidentsApi);

const sampleNodes: ComputeNode[] = [
  { id: "node-a100", name: "Alpha GPU Worker", gpuModel: "NVIDIA A100", gpuCount: 4, status: "Available", activeFault: "None" },
  { id: "node-h100", name: "Beta Training Worker", gpuModel: "NVIDIA H100", gpuCount: 8, status: "Running", activeFault: "None" },
  { id: "node-stopped", name: "Gamma Inference Worker", gpuModel: "NVIDIA A100", gpuCount: 2, status: "Stopped", activeFault: "None" },
];

describe("Fleet filters", () => {
  beforeEach(() => {
    jest.resetAllMocks();
    mockedNodesApi.getAll.mockResolvedValue(sampleNodes);
    mockedNodesApi.getHealthHistory.mockResolvedValue([]);
    mockedNodesApi.getNodeMetrics.mockResolvedValue([]);
    mockedNetworksApi.getAll.mockResolvedValue([]);
    mockedIncidentsApi.getAll.mockResolvedValue([]);
    mockedIncidentsApi.search.mockResolvedValue({ items: [], page: 1, pageSize: 12, totalCount: 0 });
  });

  it("searches nodes by name and clears filters", async () => {
    const user = userEvent.setup();
    renderWithProviders(<Home />);

    expect(await screen.findByRole("heading", { name: "Alpha GPU Worker" })).toBeInTheDocument();
    await user.type(screen.getByRole("searchbox", { name: "Search nodes" }), "beta");

    expect(screen.getByRole("heading", { name: "Beta Training Worker" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Alpha GPU Worker" })).not.toBeInTheDocument();
    expect(screen.getByText("1 matching node")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Clear filters" }));
    expect(screen.getByRole("heading", { name: "Alpha GPU Worker" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Beta Training Worker" })).toBeInTheDocument();
    expect(screen.getByText("3 matching nodes")).toBeInTheDocument();
  });

  it("filters by lifecycle status and GPU model", async () => {
    const user = userEvent.setup();
    renderWithProviders(<Home />);
    await screen.findByRole("heading", { name: "Alpha GPU Worker" });

    await user.selectOptions(screen.getByRole("combobox", { name: "Filter by status" }), "Running");
    expect(screen.getByRole("heading", { name: "Beta Training Worker" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Alpha GPU Worker" })).not.toBeInTheDocument();

    await user.selectOptions(screen.getByRole("combobox", { name: "Filter by status" }), "all");
    await user.selectOptions(screen.getByRole("combobox", { name: "Filter by GPU model" }), "NVIDIA A100");
    expect(screen.getByRole("heading", { name: "Alpha GPU Worker" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Gamma Inference Worker" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Beta Training Worker" })).not.toBeInTheDocument();
    expect(screen.getByText("2 matching nodes")).toBeInTheDocument();
  });

  it("shows an empty result state and removes pagination when filters leave no or few matches", async () => {
    const user = userEvent.setup();
    const manyNodes: ComputeNode[] = Array.from({ length: 13 }, (_, index) => ({
      id: `node-${index + 1}`,
      name: `GPU Node ${index + 1}`,
      gpuModel: index < 2 ? "NVIDIA H100" : "NVIDIA A100",
      gpuCount: 4,
      status: "Available",
      activeFault: "None",
    }));
    mockedNodesApi.getAll.mockResolvedValue(manyNodes);
    renderWithProviders(<Home />);
    await screen.findByRole("heading", { name: "GPU Node 1" });
    expect(screen.getByRole("navigation", { name: "Compute node pages" })).toBeInTheDocument();

    await user.type(screen.getByRole("searchbox", { name: "Search nodes" }), "not-a-real-node");
    expect(screen.getByRole("heading", { name: "No matching nodes" })).toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Compute node pages" })).not.toBeInTheDocument();

    await user.clear(screen.getByRole("searchbox", { name: "Search nodes" }));
    await user.selectOptions(screen.getByRole("combobox", { name: "Filter by GPU model" }), "NVIDIA H100");
    expect(screen.getByRole("heading", { name: "GPU Node 1" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "GPU Node 2" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "GPU Node 3" })).not.toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Compute node pages" })).not.toBeInTheDocument();
  });
});
