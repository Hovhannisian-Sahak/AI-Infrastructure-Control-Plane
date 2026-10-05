import { render, screen, waitFor } from "@testing-library/react";
import { Provider } from "react-redux";
import IncidentsSection from "../IncidentsSection";
import { incidentsApi } from "@/lib/api/incidentsApi";
import { createTestStore } from "@/test-utils";
import type { Incident } from "@/lib/api/models/incident";

jest.mock("@/lib/api/incidentsApi");

const mockedIncidentsApi = jest.mocked(incidentsApi);

const incident: Incident = {
  id: "incident-1",
  computeNodeId: "node-1",
  severity: "Medium",
  status: "Open",
  title: "Compute node health check failed",
  description:
    "Health check failed. CPU: 93.3%, GPU: 63.1%, GPU temperature: 78.0°C.",
  createdAt: "2026-10-05T10:00:00Z",
  updatedAt: "2026-10-05T10:00:00Z",
  resolvedAt: null,
};

const criticalIncident: Incident = {
  id: "incident-2",
  computeNodeId: "node-2",
  severity: "Critical",
  status: "Open",
  title: "GPU failure detected",
  description: "GPU failure detected on compute node.",
  createdAt: "2026-10-05T11:00:00Z",
  updatedAt: "2026-10-05T11:00:00Z",
  resolvedAt: null,
};

function renderIncidentsSection(
  preloadedIncidents: Incident[] = [],
) {
  const store = createTestStore({
    incidents: {
      incidents: preloadedIncidents,
    },
  });

  return {
    store,
    ...render(
      <Provider store={store}>
        <IncidentsSection />
      </Provider>,
    ),
  };
}

beforeEach(() => {
  jest.resetAllMocks();

  mockedIncidentsApi.getAll.mockReturnValue(
    new Promise<Incident[]>(() => {}),
  );
  mockedIncidentsApi.getById.mockReturnValue(
    new Promise<Incident>(() => {}),
  );
});

describe("IncidentsSection", () => {
  it("fetches incidents when mounted", async () => {
    renderIncidentsSection();

    await waitFor(() => {
      expect(mockedIncidentsApi.getAll).toHaveBeenCalledTimes(1);
    });
  });

  it("renders the Incidents heading", () => {
    renderIncidentsSection();

    expect(
      screen.getByRole("heading", {
        name: "Incidents",
      }),
    ).toBeInTheDocument();
  });

  it("renders zero incidents initially", () => {
    renderIncidentsSection();

    expect(screen.getByText("0 incidents")).toBeInTheDocument();
  });

  it("renders singular incident count", () => {
    renderIncidentsSection([incident]);

    expect(screen.getByText("1 incident")).toBeInTheDocument();
  });

  it("renders plural incident count", () => {
    renderIncidentsSection([
      incident,
      criticalIncident,
    ]);

    expect(screen.getByText("2 incidents")).toBeInTheDocument();
  });

  it("shows the empty state when there are no incidents", async () => {
    mockedIncidentsApi.getAll.mockResolvedValue([]);

    renderIncidentsSection();

    expect(
      await screen.findByRole("heading", {
        name: "No incidents",
      }),
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        /The fleet currently has no reported incidents/i,
      ),
    ).toBeInTheDocument();
  });

  it("renders incident cards", async () => {
    mockedIncidentsApi.getAll.mockResolvedValue([
      incident,
      criticalIncident,
    ]);

    renderIncidentsSection([
      incident,
      criticalIncident,
    ]);

    expect(
      await screen.findByRole("heading", {
        name: "Compute node health check failed",
      }),
    ).toBeInTheDocument();

    expect(
      await screen.findByRole("heading", {
        name: "GPU failure detected",
      }),
    ).toBeInTheDocument();
  });

  it("does not show the empty state when incidents exist", async () => {
    mockedIncidentsApi.getAll.mockResolvedValue([
      incident,
    ]);

    renderIncidentsSection([incident]);

    expect(
      await screen.findByRole("heading", {
        name: "Compute node health check failed",
      }),
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("heading", {
        name: "No incidents",
      }),
    ).not.toBeInTheDocument();
  });

  it("shows the loading state", () => {
    const store = createTestStore({
      incidents: {
        incidents: [],
        loading: true,
      },
    });

    render(
      <Provider store={store}>
        <IncidentsSection />
      </Provider>,
    );

    expect(
      screen.getByText("Loading incidents..."),
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("heading", {
        name: "No incidents",
      }),
    ).not.toBeInTheDocument();
  });

  it("shows the error state", async () => {
    mockedIncidentsApi.getAll.mockRejectedValue(
      new Error("Failed to load incidents"),
    );

    renderIncidentsSection();

    expect(
      await screen.findByRole("alert"),
    ).toHaveTextContent(
      "Failed to load incidents",
    );

    expect(
        screen.getByRole("heading", {
          name: "No incidents",
        }),
    ).toBeInTheDocument();
  });

  it("does not show incident cards while loading", () => {
    const store = createTestStore({
      incidents: {
        incidents: [incident],
        loading: true,
      },
    });

    render(
      <Provider store={store}>
        <IncidentsSection />
      </Provider>,
    );

    expect(
      screen.getByText("Loading incidents..."),
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("heading", {
        name: "Compute node health check failed",
      }),
    ).not.toBeInTheDocument();
  });

  it("keeps incident cards visible when there is an error", async () => {
    mockedIncidentsApi.getAll.mockRejectedValue(
        new Error("Failed to load incidents"),
    );

    const store = createTestStore({
      incidents: {
        incidents: [incident],
      },
    });

    render(
        <Provider store={store}>
          <IncidentsSection />
        </Provider>,
    );

    expect(
        await screen.findByRole("alert"),
    ).toHaveTextContent("Failed to load incidents");

    expect(
        screen.getByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).toBeInTheDocument();
  });

  it("renders incident severity and description", async () => {
    mockedIncidentsApi.getAll.mockResolvedValue([
      incident,
    ]);

    renderIncidentsSection([incident]);

    expect(
      await screen.findByText("Medium"),
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "Health check failed. CPU: 93.3%, GPU: 63.1%, GPU temperature: 78.0°C.",
      ),
    ).toBeInTheDocument();
  });
});
