import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import NetworkCard from "../NetworkCard";
import { networksApi } from "@/lib/api/networksApi";
import { createTestStore } from "@/test-utils";
import { Provider } from "react-redux";
import type { Network, NetworkAttachment } from "@/types/network";

jest.mock("@/lib/api/networksApi");

const mockedNetworksApi = jest.mocked(networksApi);

const network: Network = {
  id: "network-1",
  name: "gpu-production",
  description: "Production GPU network",
  isActive: true,
  maxAttachments: 4,
  createdAt: "2026-10-03T10:00:00Z",
};

const attachment: NetworkAttachment = {
  id: "attachment-1",
  computeNodeId: "node-1",
  networkId: "network-1",
  attachedAt: "2026-10-03T11:00:00Z",
};

const secondAttachment: NetworkAttachment = {
  id: "attachment-2",
  computeNodeId: "node-2",
  networkId: "network-1",
  attachedAt: "2026-10-03T12:00:00Z",
};

const nodes = [
  {
    id: "node-1",
    name: "GPU Node 1",
    gpuModel: "NVIDIA H100",
    gpuCount: 8,
    status: "Available" as const,
    activeFault: "None" as const,
  },
  {
    id: "node-2",
    name: "GPU Node 2",
    gpuModel: "NVIDIA A100",
    gpuCount: 4,
    status: "Available" as const,
    activeFault: "None" as const,
  },
  {
    id: "node-3",
    name: "GPU Node 3",
    gpuModel: "NVIDIA H200",
    gpuCount: 8,
    status: "Running" as const,
    activeFault: "None" as const,
  },
];

function renderNetworkCard(overrides?: { network?: Network; attachments?: NetworkAttachment[] }) {
  const store = createTestStore({
    nodes: {
      nodes,
    },
    networks: {
      networks: [],
      attachmentsByNetworkId: {
        [overrides?.network?.id ?? network.id]: {
          items: overrides?.attachments ?? [],
          loading: false,
          error: null,
          attachLoading: false,
          attachSuccess: null,
          attachError: null,
        },
      },
    },
  });

  return {
    store,
    ...render(
      <Provider store={store}>
        <NetworkCard network={overrides?.network ?? network} />
      </Provider>,
    ),
  };
}

beforeEach(() => {
  jest.resetAllMocks();

  mockedNetworksApi.getNetworkNodes.mockReturnValue(new Promise<NetworkAttachment[]>(() => {}));
  mockedNetworksApi.activate.mockReturnValue(new Promise<void>(() => {}));
  mockedNetworksApi.deactivate.mockReturnValue(new Promise<void>(() => {}));
  mockedNetworksApi.attach.mockReturnValue(new Promise<NetworkAttachment>(() => {}));
  mockedNetworksApi.detach.mockReturnValue(new Promise<void>(() => {}));
});

describe("NetworkCard", () => {
  it("renders network information", () => {
    renderNetworkCard();

    expect(
      screen.getByRole("heading", {
        name: "gpu-production",
      }),
    ).toBeInTheDocument();

    expect(screen.getByText("Production GPU network")).toBeInTheDocument();

    expect(screen.getByText("Active")).toBeInTheDocument();
    expect(screen.getByText("0 / 4")).toBeInTheDocument();
  });

  it("fetches attachments when mounted", async () => {
    mockedNetworksApi.getNetworkNodes.mockResolvedValue([]);
    const { store } = renderNetworkCard();

    await waitFor(() => {
      expect(mockedNetworksApi.getNetworkNodes).toHaveBeenCalledWith("network-1");
      expect(store.getState().networks.attachmentsByNetworkId["network-1"].loading).toBe(false);
    });
  });

  it("shows attachments when Show attachments is clicked", async () => {
    const user = userEvent.setup();
    mockedNetworksApi.getNetworkNodes.mockResolvedValue([attachment]);

    renderNetworkCard({
      attachments: [attachment],
    });

    expect(screen.getByText("1 / 4")).toBeInTheDocument();

    await user.click(
      screen.getByRole("button", {
        name: /show attachments/i,
      }),
    );

    expect(await screen.findByText("GPU Node 1")).toBeInTheDocument();

    expect(
      screen.getByRole("button", {
        name: /hide attachments/i,
      }),
    ).toBeInTheDocument();
  });

  it("hides attachments when Hide attachments is clicked", async () => {
    const user = userEvent.setup();
    mockedNetworksApi.getNetworkNodes.mockResolvedValue([attachment]);

    renderNetworkCard({
      attachments: [attachment],
    });

    await user.click(
      screen.getByRole("button", {
        name: /show attachments/i,
      }),
    );

    expect(await screen.findByText("GPU Node 1")).toBeInTheDocument();

    await user.click(
      screen.getByRole("button", {
        name: /hide attachments/i,
      }),
    );

    expect(screen.queryByText("GPU Node 1")).not.toBeInTheDocument();

    expect(
      screen.getByRole("button", {
        name: /show attachments/i,
      }),
    ).toBeInTheDocument();
  });

  it("displays the attachment count before attachments are shown", () => {
    renderNetworkCard({
      attachments: [attachment, secondAttachment],
    });

    expect(screen.getByText("2 / 4")).toBeInTheDocument();

    expect(screen.queryByText("GPU Node 1")).not.toBeInTheDocument();

    expect(screen.queryByText("GPU Node 2")).not.toBeInTheDocument();
  });

  it("does not offer nodes that are already attached", async () => {
    const user = userEvent.setup();

    renderNetworkCard({
      attachments: [attachment],
    });

    await user.click(
      screen.getByRole("button", {
        name: /show attachments/i,
      }),
    );

    const select = screen.getByRole("combobox");

    expect(select).toBeInTheDocument();

    expect(
      screen.queryByRole("option", {
        name: /GPU Node 1/,
      }),
    ).not.toBeInTheDocument();

    expect(
      screen.getByRole("option", {
        name: /GPU Node 2/,
      }),
    ).toBeInTheDocument();

    expect(
      screen.getByRole("option", {
        name: /GPU Node 3/,
      }),
    ).toBeInTheDocument();
  });

  it("attaches the selected node", async () => {
    const user = userEvent.setup();

    mockedNetworksApi.attach.mockResolvedValue({
      id: "attachment-2",
      computeNodeId: "node-2",
      networkId: "network-1",
      attachedAt: "2026-10-03T13:00:00Z",
    });

    const { store } = renderNetworkCard();

    await user.click(
      screen.getByRole("button", {
        name: /show attachments/i,
      }),
    );

    const select = screen.getByRole("combobox");

    await user.selectOptions(select, "node-2");

    await user.click(
      screen.getByRole("button", {
        name: /^attach$/i,
      }),
    );

    await waitFor(() => {
      expect(mockedNetworksApi.attach).toHaveBeenCalledWith("node-2", "network-1");
      expect(store.getState().networks.attachmentsByNetworkId["network-1"].attachLoading).toBe(
        false,
      );
    });
  });

  it("detaches an attached node", async () => {
    const user = userEvent.setup();
    mockedNetworksApi.getNetworkNodes.mockResolvedValue([attachment]);

    renderNetworkCard({
      attachments: [attachment],
    });

    await user.click(
      screen.getByRole("button", {
        name: /show attachments/i,
      }),
    );

    expect(await screen.findByText("GPU Node 1")).toBeInTheDocument();

    await user.click(
      screen.getByRole("button", {
        name: /detach/i,
      }),
    );

    await waitFor(() => {
      expect(mockedNetworksApi.detach).toHaveBeenCalledWith("node-1", "network-1");
    });
  });

  it("activates an inactive network", async () => {
    const user = userEvent.setup();

    const inactiveNetwork: Network = {
      ...network,
      isActive: false,
    };

    renderNetworkCard({
      network: inactiveNetwork,
    });

    expect(screen.getByText("Inactive")).toBeInTheDocument();

    await user.click(
      screen.getByRole("button", {
        name: /activate/i,
      }),
    );

    await waitFor(() => {
      expect(mockedNetworksApi.activate).toHaveBeenCalledWith("network-1");
    });
  });

  it("deactivates an active network", async () => {
    const user = userEvent.setup();

    renderNetworkCard();

    await user.click(
      screen.getByRole("button", {
        name: /deactivate/i,
      }),
    );

    await waitFor(() => {
      expect(mockedNetworksApi.deactivate).toHaveBeenCalledWith("network-1");
    });
  });

  it("prevents attaching nodes when the network is inactive", () => {
    const inactiveNetwork: Network = {
      ...network,
      isActive: false,
    };

    renderNetworkCard({
      network: inactiveNetwork,
    });

    expect(screen.getByText(/activate this network before attaching nodes/i)).toBeInTheDocument();

    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();

    expect(
      screen.queryByRole("button", {
        name: /^attach$/i,
      }),
    ).not.toBeInTheDocument();
  });

  it("prevents attaching nodes when maximum capacity is reached", () => {
    const fullNetwork: Network = {
      ...network,
      maxAttachments: 2,
    };

    renderNetworkCard({
      network: fullNetwork,
      attachments: [attachment, secondAttachment],
    });

    expect(screen.getByText(/maximum attachment limit reached/i)).toBeInTheDocument();

    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();

    expect(
      screen.queryByRole("button", {
        name: /^attach$/i,
      }),
    ).not.toBeInTheDocument();
  });

  it("shows all nodes attached when every node is already attached", () => {
    const allAttachments: NetworkAttachment[] = [
      {
        id: "attachment-1",
        computeNodeId: "node-1",
        networkId: "network-1",
        attachedAt: "2026-10-03T11:00:00Z",
      },
      {
        id: "attachment-2",
        computeNodeId: "node-2",
        networkId: "network-1",
        attachedAt: "2026-10-03T12:00:00Z",
      },
      {
        id: "attachment-3",
        computeNodeId: "node-3",
        networkId: "network-1",
        attachedAt: "2026-10-03T13:00:00Z",
      },
    ];

    renderNetworkCard({
      attachments: allAttachments,
    });

    expect(screen.getByText(/all nodes are already attached/i)).toBeInTheDocument();

    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
  });

  it("shows attach loading state", () => {
    const store = createTestStore({
      nodes: {
        nodes,
      },
      networks: {
        networks: [],
        attachmentsByNetworkId: {
          "network-1": {
            items: [],
            loading: false,
            error: null,
            attachLoading: true,
            attachSuccess: null,
            attachError: null,
          },
        },
      },
    });

    render(
      <Provider store={store}>
        <NetworkCard network={network} />
      </Provider>,
    );

    expect(
      screen.getByRole("button", {
        name: /attaching/i,
      }),
    ).toBeDisabled();
  });

  it("shows attachment error", () => {
    const store = createTestStore({
      nodes: {
        nodes,
      },
      networks: {
        networks: [],
        attachmentsByNetworkId: {
          "network-1": {
            items: [],
            loading: false,
            error: null,
            attachLoading: false,
            attachSuccess: null,
            attachError: "Failed to attach node.",
          },
        },
      },
    });

    render(
      <Provider store={store}>
        <NetworkCard network={network} />
      </Provider>,
    );

    expect(screen.getByRole("alert")).toHaveTextContent("Failed to attach node.");
  });

  it("shows attachment success message", () => {
    const store = createTestStore({
      nodes: {
        nodes,
      },
      networks: {
        networks: [],
        attachmentsByNetworkId: {
          "network-1": {
            items: [attachment],
            loading: false,
            error: null,
            attachLoading: false,
            attachSuccess: "Node attached successfully.",
            attachError: null,
          },
        },
      },
    });

    render(
      <Provider store={store}>
        <NetworkCard network={network} />
      </Provider>,
    );

    expect(screen.getByRole("status")).toHaveTextContent("Node attached successfully.");
  });
});
