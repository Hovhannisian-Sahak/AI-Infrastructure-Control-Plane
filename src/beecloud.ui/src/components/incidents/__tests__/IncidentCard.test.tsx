import { render, screen } from "@testing-library/react";
import IncidentCard from "../IncidentCard";
import type { Incident } from "@/lib/api/models/incident";

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

describe("IncidentCard", () => {
  it("renders the incident title", () => {
    render(<IncidentCard incident={incident} />);

    expect(
      screen.getByRole("heading", {
        name: "Compute node health check failed",
      }),
    ).toBeInTheDocument();
  });

  it("links the affected node to its detail page", () => {
    render(<IncidentCard incident={incident} />);

    expect(screen.getByRole("link", { name: /Affected node node-1/ }))
      .toHaveAttribute("href", "/nodes/node-1");
  });

  it("shows a friendly node name when provided", () => {
    render(<IncidentCard incident={incident} nodeName="GPU Worker 01" />);

    expect(screen.getByRole("link", { name: /Affected node GPU Worker 01/ }))
      .toHaveAttribute("href", "/nodes/node-1");
    expect(screen.queryByText("node-1")).not.toBeInTheDocument();
  });

  it("renders the incident severity", () => {
    render(<IncidentCard incident={incident} />);

    expect(
      screen.getByText("Medium"),
    ).toBeInTheDocument();
  });

  it("renders the incident status", () => {
    render(<IncidentCard incident={incident} />);

    expect(
      screen.getByText("Open"),
    ).toBeInTheDocument();
  });

  it("renders the incident description", () => {
    render(<IncidentCard incident={incident} />);

    expect(
      screen.getByText(
        "Health check failed. CPU: 93.3%, GPU: 63.1%, GPU temperature: 78.0°C.",
      ),
    ).toBeInTheDocument();
  });

  it("does not render the description when it is missing", () => {
    const incidentWithoutDescription: Incident = {
      ...incident,
      description: null,
    };

    render(
      <IncidentCard
        incident={incidentWithoutDescription}
      />,
    );

    expect(
      screen.queryByText(
        "Health check failed. CPU: 93.3%, GPU: 63.1%, GPU temperature: 78.0°C.",
      ),
    ).not.toBeInTheDocument();
  });

  it("renders the incident creation time", () => {
    render(<IncidentCard incident={incident} />);

    expect(
      screen.getByRole("time"),
    ).toHaveAttribute(
      "dateTime",
      "2026-10-05T10:00:00Z",
    );
  });

  it("shows the latest activity time when the incident has been updated", () => {
    render(
      <IncidentCard
        incident={{
          ...incident,
          updatedAt: "2026-10-05T11:00:00Z",
        }}
      />,
    );

    expect(screen.getByText(/Updated/)).toBeInTheDocument();
    expect(
      screen.getAllByRole("time").some(
        time => time.getAttribute("dateTime") === "2026-10-05T11:00:00Z",
      ),
    ).toBe(true);
  });

  it("labels resolved activity using the resolution timestamp", () => {
    render(
      <IncidentCard
        incident={{
          ...incident,
          status: "Resolved",
          resolvedAt: "2026-10-05T12:00:00Z",
          updatedAt: "2026-10-05T12:00:00Z",
        }}
      />,
    );

    expect(screen.getByText("Resolved", { selector: "span.status" }))
      .toBeInTheDocument();
    expect(
      screen.getAllByRole("time").some(
        time => time.getAttribute("dateTime") === "2026-10-05T12:00:00Z",
      ),
    ).toBe(true);
  });

  it("renders critical severity", () => {
    const criticalIncident: Incident = {
      ...incident,
      severity: "Critical",
    };

    render(
      <IncidentCard
        incident={criticalIncident}
      />,
    );

    expect(
      screen.getByText("Critical"),
    ).toBeInTheDocument();
  });
});
