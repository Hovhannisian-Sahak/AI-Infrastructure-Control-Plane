import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Provider } from "react-redux";
import NetworksSection from "../NetworksSection";
import { networksApi } from "@/lib/api/networksApi";
import { createTestStore } from "@/test-utils";
import type { Network } from "@/types/network";

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

const secondNetwork: Network = {
  id: "network-2",
  name: "gpu-development",
  description: "Development GPU network",
  isActive: false,
  maxAttachments: 4,
  createdAt: "2026-10-03T11:00:00Z",
};

function renderNetworksSection(preloadedNetworks: Network[] = []) {
  const store = createTestStore({
    networks: {
      networks: preloadedNetworks,
    },
  });

  return {
    store,
    ...render(
      <Provider store={store}>
        <NetworksSection />
      </Provider>,
    ),
  };
}

beforeEach(() => {
  jest.resetAllMocks();

  mockedNetworksApi.getAll.mockReturnValue(new Promise<Network[]>(() => {}));
  mockedNetworksApi.create.mockResolvedValue(network);
  mockedNetworksApi.getNetworkNodes.mockReturnValue(new Promise(() => {}));
});

describe("NetworksSection", () => {
  it("fetches networks when mounted", async () => {
    renderNetworksSection();

    await waitFor(() => {
      expect(mockedNetworksApi.getAll).toHaveBeenCalledTimes(1);
    });
  });

  it("renders the Networks heading", () => {
    renderNetworksSection();

    expect(
      screen.getByRole("heading", {
        name: "Networks",
      }),
    ).toBeInTheDocument();
  });

  it("renders zero networks initially", () => {
    renderNetworksSection();

    expect(screen.getByText("0 networks")).toBeInTheDocument();
  });

  it("renders singular network count", () => {
    renderNetworksSection([network]);

    expect(screen.getByText("1 network")).toBeInTheDocument();
  });

  it("renders plural network count", () => {
    renderNetworksSection([network, secondNetwork]);

    expect(screen.getByText("2 networks")).toBeInTheDocument();
  });

  it("renders the create network form", () => {
    renderNetworksSection();

    expect(
      screen.getByRole("heading", {
        name: "Create Network",
      }),
    ).toBeInTheDocument();

    expect(screen.getByLabelText("Network name")).toBeInTheDocument();

    expect(screen.getByLabelText(/Description/)).toBeInTheDocument();

    expect(
      screen.getByRole("button", {
        name: "Create Network",
      }),
    ).toBeInTheDocument();
  });

  it("shows the empty state when there are no networks", async () => {
    mockedNetworksApi.getAll.mockResolvedValue([]);

    renderNetworksSection();

    expect(
      await screen.findByRole("heading", {
        name: "No networks yet",
      }),
    ).toBeInTheDocument();

    expect(
      screen.getByText(/Create your first network to connect compute nodes/i),
    ).toBeInTheDocument();
  });

  it("renders network cards", async () => {
    mockedNetworksApi.getAll.mockResolvedValue([network, secondNetwork]);

    renderNetworksSection([network, secondNetwork]);

    expect(
      await screen.findByRole("heading", {
        name: "gpu-production",
      }),
    ).toBeInTheDocument();

    expect(
      await screen.findByRole("heading", {
        name: "gpu-development",
      }),
    ).toBeInTheDocument();
  });

  it("does not show the empty state when networks exist", async () => {
    mockedNetworksApi.getAll.mockResolvedValue([network]);

    renderNetworksSection([network]);

    expect(
      await screen.findByRole("heading", {
        name: "gpu-production",
      }),
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("heading", {
        name: "No networks yet",
      }),
    ).not.toBeInTheDocument();
  });

  it("shows the loading state", () => {
    const store = createTestStore({
      networks: {
        networks: [],
        loading: true,
      },
    });

    render(
      <Provider store={store}>
        <NetworksSection />
      </Provider>,
    );

    expect(screen.getByText("Loading networks...")).toBeInTheDocument();

    expect(
      screen.queryByRole("heading", {
        name: "No networks yet",
      }),
    ).not.toBeInTheDocument();
  });

  it("shows the error state", async () => {
    mockedNetworksApi.getAll.mockRejectedValue(new Error("Failed to load networks"));

    renderNetworksSection();

    expect(await screen.findByRole("alert")).toHaveTextContent("Failed to load networks");

    expect(
      screen.queryByRole("heading", {
        name: "No networks yet",
      }),
    ).not.toBeInTheDocument();
  });

  it("does not show network cards while loading", () => {
    const store = createTestStore({
      networks: {
        networks: [network],
        loading: true,
      },
    });

    render(
      <Provider store={store}>
        <NetworksSection />
      </Provider>,
    );

    expect(screen.getByText("Loading networks...")).toBeInTheDocument();

    expect(
      screen.queryByRole("heading", {
        name: "gpu-production",
      }),
    ).not.toBeInTheDocument();
  });

  it("keeps network cards visible when there is an error", async () => {
    mockedNetworksApi.getAll.mockRejectedValue(
        new Error("Failed to load networks"),
    );

    const store = createTestStore({
      networks: {
        networks: [network],
      },
    });

    render(
        <Provider store={store}>
          <NetworksSection />
        </Provider>,
    );

    expect(
        await screen.findByRole("alert"),
    ).toHaveTextContent("Failed to load networks");

    expect(
        screen.getByRole("heading", {
          name: "gpu-production",
        }),
    ).toBeInTheDocument();
  });

  it("creates a network from the form", async () => {
    const user = userEvent.setup();

    mockedNetworksApi.create.mockResolvedValue(network);

    renderNetworksSection();

    const nameInput = screen.getByLabelText("Network name");

    const descriptionInput = screen.getByLabelText(/Description/);

    await user.type(nameInput, "gpu-production");

    await user.type(descriptionInput, "Production GPU network");

    await user.click(
      screen.getByRole("button", {
        name: "Create Network",
      }),
    );

    await waitFor(() => {
      expect(mockedNetworksApi.create).toHaveBeenCalledWith({
        name: "gpu-production",
        description: "Production GPU network",
      });
    });
  });

  it("shows create success message", async () => {
    const user = userEvent.setup();

    mockedNetworksApi.create.mockResolvedValue(network);

    renderNetworksSection();

    await user.type(screen.getByLabelText("Network name"), "gpu-production");

    await user.click(
      screen.getByRole("button", {
        name: "Create Network",
      }),
    );

    await waitFor(() => {
      expect(screen.getByRole("status")).toHaveTextContent(
        'Network "gpu-production" created successfully.',
      );
    });
  });
});
