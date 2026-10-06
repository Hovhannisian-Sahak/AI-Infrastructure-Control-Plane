import {
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
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
      await screen.findByText("Medium", { selector: "span" }),
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "Health check failed. CPU: 93.3%, GPU: 63.1%, GPU temperature: 78.0°C.",
      ),
    ).toBeInTheDocument();
  });

  it("filters incidents by severity", async () => {
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
        screen.getByRole("heading", {
          name: "GPU failure detected",
        }),
    ).toBeInTheDocument();

    const severityFilter = screen.getByLabelText(
        "Severity",
    );

    fireEvent.change(severityFilter, {
      target: {
        value: "Critical",
      },
    });

    expect(
        screen.getByRole("heading", {
          name: "GPU failure detected",
        }),
    ).toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByText("1 incident"),
    ).toBeInTheDocument();
  });

  it("filters incidents by status", async () => {
    const resolvedIncident: Incident = {
      ...incident,
      id: "incident-3",
      status: "Resolved",
      title: "Resolved health check incident",
    };

    mockedIncidentsApi.getAll.mockResolvedValue([
      incident,
      resolvedIncident,
    ]);

    renderIncidentsSection([
      incident,
      resolvedIncident,
    ]);

    expect(
        await screen.findByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("heading", {
          name: "Resolved health check incident",
        }),
    ).toBeInTheDocument();

    const statusFilter = screen.getByLabelText("Status");

    fireEvent.change(statusFilter, {
      target: {
        value: "Resolved",
      },
    });

    expect(
        screen.getByRole("heading", {
          name: "Resolved health check incident",
        }),
    ).toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByText("1 incident"),
    ).toBeInTheDocument();
  });

  it("filters incidents by node", async () => {
    mockedIncidentsApi.getAll.mockResolvedValue([
      incident,
      criticalIncident,
    ]);

    const store = createTestStore({
      incidents: {
        incidents: [
          incident,
          criticalIncident,
        ],
      },
      nodes: {
        nodes: [
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
            status: "Running",
            activeFault: "None",
          },
        ],
      },
    });

    render(
        <Provider store={store}>
          <IncidentsSection />
        </Provider>,
    );

    expect(
        await screen.findByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("heading", {
          name: "GPU failure detected",
        }),
    ).toBeInTheDocument();

    const nodeFilter = screen.getByLabelText("Node");

    fireEvent.change(nodeFilter, {
      target: {
        value: "node-2",
      },
    });

    expect(
        screen.getByRole("heading", {
          name: "GPU failure detected",
        }),
    ).toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByText("1 incident"),
    ).toBeInTheDocument();
  });
  
  it("filters incidents from a selected date", async () => {
    const previousDayIncident: Incident = {
      ...incident,
      id: "incident-3",
      title: "Previous day incident",
      createdAt: "2026-10-04T10:00:00Z",
    };

    mockedIncidentsApi.getAll.mockResolvedValue([
      previousDayIncident,
      incident,
    ]);

    renderIncidentsSection([
      previousDayIncident,
      incident,
    ]);

    expect(
        await screen.findByRole("heading", {
          name: "Previous day incident",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).toBeInTheDocument();

    const fromDateFilter = screen.getByLabelText("From");

    fireEvent.change(fromDateFilter, {
      target: {
        value: "2026-10-05",
      },
    });

    expect(
        screen.getByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Previous day incident",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByText("1 incident"),
    ).toBeInTheDocument();
  });

  it("filters incidents up to a selected date", async () => {
    const nextDayIncident: Incident = {
      ...incident,
      id: "incident-3",
      title: "Next day incident",
      createdAt: "2026-10-06T10:00:00Z",
    };

    mockedIncidentsApi.getAll.mockResolvedValue([
      incident,
      nextDayIncident,
    ]);

    renderIncidentsSection([
      incident,
      nextDayIncident,
    ]);

    expect(
        await screen.findByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("heading", {
          name: "Next day incident",
        }),
    ).toBeInTheDocument();

    const toDateFilter = screen.getByLabelText("To");

    fireEvent.change(toDateFilter, {
      target: {
        value: "2026-10-05",
      },
    });

    expect(
        screen.getByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Next day incident",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByText("1 incident"),
    ).toBeInTheDocument();
  });

  it("applies multiple filters together", async () => {
    const matchingIncident: Incident = {
      ...criticalIncident,
      id: "incident-3",
      computeNodeId: "node-2",
      severity: "Critical",
      status: "Open",
      createdAt: "2026-10-05T11:00:00Z",
    };

    const wrongSeverityIncident: Incident = {
      ...matchingIncident,
      id: "incident-4",
      title: "Wrong severity incident",
      severity: "High",
    };

    const wrongStatusIncident: Incident = {
      ...matchingIncident,
      id: "incident-5",
      title: "Wrong status incident",
      status: "Resolved",
    };

    const wrongNodeIncident: Incident = {
      ...matchingIncident,
      id: "incident-6",
      title: "Wrong node incident",
      computeNodeId: "node-1",
    };

    const wrongDateIncident: Incident = {
      ...matchingIncident,
      id: "incident-7",
      title: "Wrong date incident",
      createdAt: "2026-10-06T10:00:00Z",
    };

    const incidents = [
      matchingIncident,
      wrongSeverityIncident,
      wrongStatusIncident,
      wrongNodeIncident,
      wrongDateIncident,
    ];

    mockedIncidentsApi.getAll.mockResolvedValue(incidents);

    const store = createTestStore({
      incidents: {
        incidents,
      },
      nodes: {
        nodes: [
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
            status: "Running",
            activeFault: "None",
          },
        ],
      },
    });

    render(
        <Provider store={store}>
          <IncidentsSection />
        </Provider>,
    );

    expect(
        await screen.findByRole("heading", {
          name: "Wrong date incident",
        }),
    ).toBeInTheDocument();

    fireEvent.change(
        screen.getByLabelText("Severity"),
        {
          target: {
            value: "Critical",
          },
        },
    );

    fireEvent.change(
        screen.getByLabelText("Status"),
        {
          target: {
            value: "Open",
          },
        },
    );

    fireEvent.change(
        screen.getByLabelText("Node"),
        {
          target: {
            value: "node-2",
          },
        },
    );

    fireEvent.change(
        screen.getByLabelText("From"),
        {
          target: {
            value: "2026-10-05",
          },
        },
    );

    fireEvent.change(
        screen.getByLabelText("To"),
        {
          target: {
            value: "2026-10-05",
          },
        },
    );

    expect(
        screen.getByRole("heading", {
          name: "GPU failure detected",
        }),
    ).toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Wrong severity incident",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Wrong status incident",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Wrong node incident",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "Wrong date incident",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByText("1 incident"),
    ).toBeInTheDocument();
  });

  it("clears all incident filters", async () => {
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

    fireEvent.change(
        screen.getByLabelText("Severity"),
        {
          target: {
            value: "Critical",
          },
        },
    );

    expect(
        screen.queryByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByRole("heading", {
          name: "GPU failure detected",
        }),
    ).toBeInTheDocument();

    fireEvent.click(
        screen.getByRole("button", {
          name: "Clear filters",
        }),
    );

    expect(
        screen.getByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("heading", {
          name: "GPU failure detected",
        }),
    ).toBeInTheDocument();

    expect(
        screen.getByText("2 incidents"),
    ).toBeInTheDocument();
  });
  it("shows a no matching incidents state when filters match nothing", async () => {
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

    fireEvent.change(
        screen.getByLabelText("Severity"),
        {
          target: {
            value: "Low",
          },
        },
    );

    expect(
        screen.queryByRole("heading", {
          name: "Compute node health check failed",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.queryByRole("heading", {
          name: "GPU failure detected",
        }),
    ).not.toBeInTheDocument();

    expect(
        screen.getByText("No incidents match the selected filters."),
    ).toBeInTheDocument();

    expect(
        screen.getByRole("button", {
          name: "Clear filters",
        }),
    ).toBeInTheDocument();
  });
});
