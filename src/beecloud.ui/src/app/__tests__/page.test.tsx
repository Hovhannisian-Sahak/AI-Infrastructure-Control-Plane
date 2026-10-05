import {
  act,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test-utils";
import CreateNodeForm from "@/components/nodes/CreateNodeForm";
import Home from "../page";
import { nodesApi } from "@/lib/api/nodesApi";
import { networksApi } from "@/lib/api/networksApi";
import type { ComputeNode } from "@/lib/api/models/computeNode";

jest.mock("@/lib/api/nodesApi");
jest.mock("@/lib/api/networksApi");

const mockedNodesApi = jest.mocked(nodesApi);
const mockedNetworksApi = jest.mocked(networksApi);

describe("Home page", () => {
  beforeEach(() => {
    jest.resetAllMocks();

    mockedNetworksApi.getAll.mockResolvedValue([]);
  });

  it("renders the page heading", async () => {
    mockedNodesApi.getAll.mockResolvedValue([]);

    renderWithProviders(<Home />);

    expect(
        screen.getByRole("heading", {
          name: "BeeCloud Nodes",
        }),
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

    expect(
        await screen.findByRole("heading", {
          name: "GPU Node 1",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getAllByText("GPU Model"),
    ).toHaveLength(2);

    expect(
        screen.getByText("NVIDIA A100"),
    ).toBeInTheDocument();

    expect(
        screen.getAllByText("GPU Count"),
    ).toHaveLength(2);

    expect(
        screen.getByText("4"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("Available"),
    ).toBeInTheDocument();
  });

  it("renders an error when the API request fails", async () => {
    mockedNodesApi.getAll.mockRejectedValue(
        new Error("API unavailable"),
    );

    renderWithProviders(<Home />);

    const errorMessage =
        await screen.findByRole("alert");

    expect(errorMessage).toHaveTextContent(
        "API unavailable",
    );

    expect(errorMessage).toHaveClass(
        "error",
    );
  });

  it("creates a node from the form", async () => {
    const user = userEvent.setup();

    mockedNodesApi.getAll.mockResolvedValue([]);

    mockedNodesApi.create.mockResolvedValue({
      id: "node-2",
      name: "GPU Node 2",
      gpuModel: "NVIDIA H100",
      gpuCount: 8,
      status: "Provisioning",
      activeFault: "None",
    });

    renderWithProviders(<Home />);

    await waitFor(() => {
      expect(
          mockedNodesApi.getAll,
      ).toHaveBeenCalledTimes(1);
    });

    await user.type(
        screen.getByRole("textbox", {
          name: "Node Name",
        }),
        "GPU Node 2",
    );

    await user.type(
        screen.getByRole("textbox", {
          name: "GPU Model",
        }),
        "NVIDIA H100",
    );

    const gpuCount =
        screen.getByRole("spinbutton", {
          name: "GPU Count",
        });

    await user.clear(gpuCount);
    await user.type(gpuCount, "8");

    await user.click(
        screen.getByRole("button", {
          name: "Create Node",
        }),
    );

    await waitFor(() => {
      expect(
          mockedNodesApi.create,
      ).toHaveBeenCalledWith({
        name: "GPU Node 2",
        gpuModel: "NVIDIA H100",
        gpuCount: 8,
      });
    });

    expect(
        await screen.findByRole("heading", {
          name: "GPU Node 2",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByText("NVIDIA H100"),
    ).toBeInTheDocument();

    expect(
        screen.getByText("Provisioning"),
    ).toBeInTheDocument();

    expect(
        screen.getByText(
            "Node created successfully.",
        ),
    ).toBeInTheDocument();
  });

  it("clears the creation success message when a later creation fails", async () => {
    const user = userEvent.setup();

    mockedNodesApi.getAll.mockResolvedValue([]);

    mockedNodesApi.create
        .mockResolvedValueOnce({
          id: "node-2",
          name: "GPU Node 2",
          gpuModel: "NVIDIA H100",
          gpuCount: 8,
          status: "Provisioning",
          activeFault: "None",
        })
        .mockRejectedValueOnce(
            new Error("Node name already exists"),
        );

    renderWithProviders(<Home />);

    await waitFor(() => {
      expect(
          mockedNodesApi.getAll,
      ).toHaveBeenCalledTimes(1);
    });

    const nameInput =
        screen.getByRole("textbox", {
          name: "Node Name",
        });

    const gpuModelInput =
        screen.getByRole("textbox", {
          name: "GPU Model",
        });

    const gpuCountInput =
        screen.getByRole("spinbutton", {
          name: "GPU Count",
        });

    await user.type(
        nameInput,
        "GPU Node 2",
    );

    await user.type(
        gpuModelInput,
        "NVIDIA H100",
    );

    await user.clear(gpuCountInput);
    await user.type(gpuCountInput, "8");

    await user.click(
        screen.getByRole("button", {
          name: "Create Node",
        }),
    );

    expect(
        await screen.findByText(
            "Node created successfully.",
        ),
    ).toBeInTheDocument();

    await user.clear(nameInput);
    await user.type(
        nameInput,
        "GPU Node 2",
    );

    await user.click(
        screen.getByRole("button", {
          name: "Create Node",
        }),
    );

    expect(
        await screen.findByRole("alert"),
    ).toHaveTextContent(
        "Node name already exists",
    );

    expect(
        screen.queryByText(
            "Node created successfully.",
        ),
    ).not.toBeInTheDocument();
  });

  it("renders an error when creating a node fails", async () => {
    const user = userEvent.setup();

    mockedNodesApi.getAll.mockResolvedValue([]);

    mockedNodesApi.create.mockRejectedValue(
        new Error(
            "A compute node with this name already exists.",
        ),
    );

    renderWithProviders(<Home />);

    await user.type(
        screen.getByRole("textbox", {
          name: "Node Name",
        }),
        "GPU Node 1",
    );

    await user.type(
        screen.getByRole("textbox", {
          name: "GPU Model",
        }),
        "NVIDIA A100",
    );

    await user.click(
        screen.getByRole("button", {
          name: "Create Node",
        }),
    );

    expect(
        await screen.findByRole("alert"),
    ).toHaveTextContent(
        "A compute node with this name already exists.",
    );
  });

  it("disables creation when the node name is empty", () => {
    const onSubmit = jest.fn();

    render(
        <CreateNodeForm
            onSubmit={onSubmit}
            isSubmitting={false}
        />,
    );

    expect(
        screen.getByRole("button", {
          name: "Create Node",
        }),
    ).toBeDisabled();

    expect(
        onSubmit,
    ).not.toHaveBeenCalled();
  });

  it("refreshes nodes when the refresh button is clicked", async () => {
    const user = userEvent.setup();

    mockedNodesApi.getAll
        .mockResolvedValueOnce([])
        .mockResolvedValueOnce([
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

    await waitFor(() => {
      expect(
          mockedNodesApi.getAll,
      ).toHaveBeenCalledTimes(1);
    });

    await user.click(
        screen.getByRole("button", {
          name: "Refresh",
        }),
    );

    await waitFor(() => {
      expect(
          mockedNodesApi.getAll,
      ).toHaveBeenCalledTimes(2);
    });

    expect(
        await screen.findByRole("heading", {
          name: "GPU Node 1",
        }),
    ).toBeInTheDocument();
  });

  it("shows an empty state when no nodes exist", async () => {
    mockedNodesApi.getAll.mockResolvedValue([]);

    renderWithProviders(<Home />);

    expect(
        await screen.findByRole("heading", {
          name: "No compute nodes",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByText(
            "Create a node to start managing your GPU fleet.",
        ),
    ).toBeInTheDocument();
  });

  it("displays singular node count", async () => {
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

    expect(
        await screen.findByText("1 node"),
    ).toBeInTheDocument();
  });

  it("displays plural node count", async () => {
    mockedNodesApi.getAll.mockResolvedValue([
      {
        id: "node-1",
        name: "GPU Node 1",
        gpuModel: "NVIDIA A100",
        gpuCount: 4,
        status: "Available",
        activeFault: "None",
      },
      {
        id: "node-2",
        name: "GPU Node 2",
        gpuModel: "NVIDIA H100",
        gpuCount: 8,
        status: "Available",
        activeFault: "None",
      },
    ]);

    renderWithProviders(<Home />);

    expect(
        await screen.findByText("2 nodes"),
    ).toBeInTheDocument();
  });


  it("shows a loading state while nodes are being fetched", async () => {
    let resolveNodes:
        | ((value: ComputeNode[]) => void)
        | undefined;

    const nodesPromise = new Promise<ComputeNode[]>(
        (resolve) => {
          resolveNodes = resolve;
        },
    );

    mockedNodesApi.getAll.mockReturnValue(nodesPromise);

    renderWithProviders(<Home />);

    expect(
        await screen.findByText("Loading nodes..."),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("button", {
          name: "Refreshing...",
        }),
    ).toBeDisabled();

    await act(async () => {
      resolveNodes!([]);
      await nodesPromise;
    });

    await waitFor(() => {
      expect(
          screen.queryByText("Loading nodes..."),
      ).not.toBeInTheDocument();
    });
  });
});