import {
    act,
    render,
    screen,
    within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import HealthAlerts from "../HealthAlerts";
import type {
    DerivedHealthAlerts,
    HealthAlert,
} from "@/lib/health/healthAlerts";

jest.mock("next/link", () => {
    return function MockLink({
                                 children,
                                 href,
                                 ...props
                             }: {
        children: React.ReactNode;
        href: string;
        [key: string]: unknown;
    }) {
        return (
            <a href={href} {...props}>
                {children}
            </a>
        );
    };
});

function createAlert(
    overrides: Partial<HealthAlert> = {},
): HealthAlert {
    const id = overrides.id ?? "alert-1";

    return {
        id,
        nodeId:
            overrides.nodeId ??
            `node-${id}`,
        nodeName:
            overrides.nodeName ??
            `GPU Node ${id}`,
        type:
            overrides.type ??
            "high-cpu",
        message:
            overrides.message ??
            "CPU usage is high (95.0%).",
        checkedAt:
            overrides.checkedAt ??
            "2026-10-07T12:00:00Z",
        ...overrides,
    };
}

function createAlerts(
    count: number,
    prefix = "alert",
): HealthAlert[] {
    return Array.from(
        { length: count },
        (_, index) =>
            createAlert({
                id: `${prefix}-${index + 1}`,
                nodeId: `${prefix}-node-${index + 1}`,
                nodeName:
                    prefix === "historical"
                        ? `Historical Node ${index + 1}`
                        : `GPU Node ${index + 1}`,
                checkedAt: `2026-10-07T12:${String(
                    index,
                ).padStart(2, "0")}:00Z`,
            }),
    );
}

function renderAlerts(
    overrides: Partial<DerivedHealthAlerts> = {},
) {
    const alerts: DerivedHealthAlerts = {
        active: [],
        historical: [],
        ...overrides,
    };

    return render(
        <HealthAlerts alerts={alerts} />,
    );
}

function getActiveSection() {
    const heading = screen.getByRole(
        "heading",
        {
            name: "Active",
        },
    );

    return heading.closest(
        "section",
    ) as HTMLElement;
}

function getHistorySection() {
    const heading = screen.getByRole(
        "heading",
        {
            name: "History",
        },
    );

    return heading.closest(
        "section",
    ) as HTMLElement;
}

/*
 * Transition an existing alert from active
 * to recently-cleared.
 *
 * The component detects the transition in
 * useEffect, so we flush React and then give
 * the effect/state update one additional act
 * cycle to complete.
 */
function clearActiveAlerts(
    rerender: ReturnType<typeof render>["rerender"],
) {
    act(() => {
        rerender(
            <HealthAlerts
                alerts={{
                    active: [],
                    historical: [],
                }}
            />,
        );
    });

    act(() => {
        jest.advanceTimersByTime(0);
    });
}

describe("HealthAlerts", () => {
    beforeEach(() => {
        jest.useFakeTimers();

        jest.setSystemTime(
            new Date(
                "2026-10-07T14:00:00Z",
            ),
        );
    });

    afterEach(() => {
        act(() => {
            jest.runOnlyPendingTimers();
        });

        jest.useRealTimers();
    });

    it("renders the active alerts section", () => {
        renderAlerts({
            active: [
                createAlert({
                    id: "active-1",
                    nodeName: "GPU Node 1",
                }),
            ],
        });

        expect(
            screen.getByRole("heading", {
                name: "Health Alerts",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByRole("heading", {
                name: "Active",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByRole("heading", {
                name: "History",
            }),
        ).toBeInTheDocument();

        expect(
            within(
                getActiveSection(),
            ).getByText("GPU Node 1"),
        ).toBeInTheDocument();
    });

    it("shows the empty state when there are no active alerts", () => {
        renderAlerts();

        expect(
            screen.getByText(
                "No active health alerts.",
            ),
        ).toBeInTheDocument();
    });

    it("shows five active alerts per page", () => {
        renderAlerts({
            active: createAlerts(7),
        });

        const activeSection =
            getActiveSection();

        expect(
            within(activeSection).getByText(
                "GPU Node 1",
            ),
        ).toBeInTheDocument();

        expect(
            within(activeSection).getByText(
                "GPU Node 5",
            ),
        ).toBeInTheDocument();

        expect(
            within(activeSection).queryByText(
                "GPU Node 6",
            ),
        ).not.toBeInTheDocument();

        expect(
            within(activeSection).queryByText(
                "GPU Node 7",
            ),
        ).not.toBeInTheDocument();

        expect(
            within(activeSection).getByRole(
                "button",
                {
                    name: /next/i,
                },
            ),
        ).toBeInTheDocument();
    });

    it("navigates to the second active-alert page", async () => {
        const user =
            userEvent.setup({
                advanceTimers:
                jest.advanceTimersByTime,
            });

        renderAlerts({
            active: createAlerts(7),
        });

        const activeSection =
            getActiveSection();

        await user.click(
            within(activeSection).getByRole(
                "button",
                {
                    name: /next/i,
                },
            ),
        );

        expect(
            within(activeSection).queryByText(
                "GPU Node 1",
            ),
        ).not.toBeInTheDocument();

        expect(
            within(activeSection).queryByText(
                "GPU Node 5",
            ),
        ).not.toBeInTheDocument();

        expect(
            within(activeSection).getByText(
                "GPU Node 6",
            ),
        ).toBeInTheDocument();

        expect(
            within(activeSection).getByText(
                "GPU Node 7",
            ),
        ).toBeInTheDocument();
    });

    it("disables previous on the first page and next on the last page", async () => {
        const user =
            userEvent.setup({
                advanceTimers:
                jest.advanceTimersByTime,
            });

        renderAlerts({
            active: createAlerts(7),
        });

        const activeSection =
            getActiveSection();

        const previousButton =
            within(activeSection).getByRole(
                "button",
                {
                    name: /previous/i,
                },
            );

        const nextButton =
            within(activeSection).getByRole(
                "button",
                {
                    name: /next/i,
                },
            );

        expect(
            previousButton,
        ).toBeDisabled();

        expect(
            nextButton,
        ).not.toBeDisabled();

        await user.click(nextButton);

        expect(
            previousButton,
        ).not.toBeDisabled();

        expect(
            nextButton,
        ).toBeDisabled();
    });

    it("does not render pagination when five or fewer active alerts exist", () => {
        renderAlerts({
            active: createAlerts(5),
        });

        const activeSection =
            getActiveSection();

        expect(
            within(activeSection).queryByRole(
                "button",
                {
                    name: /next/i,
                },
            ),
        ).not.toBeInTheDocument();

        expect(
            within(activeSection).queryByRole(
                "button",
                {
                    name: /previous/i,
                },
            ),
        ).not.toBeInTheDocument();
    });

    it("renders historical alerts separately with their historical context", () => {
        renderAlerts({
            historical: [
                createAlert({
                    id: "historical-1",
                    nodeName:
                        "Historical Node",
                    checkedAt:
                        "2026-10-06T12:00:00Z",
                    message:
                        "GPU temperature reached 94.0°C.",
                }),
            ],
        });

        const historySection =
            getHistorySection();

        expect(
            within(historySection).getByText(
                "Historical Node",
            ),
        ).toBeInTheDocument();

        expect(
            within(historySection).getByText(
                "Yesterday",
            ),
        ).toBeInTheDocument();

        expect(
            within(historySection).getByText(
                "GPU temperature reached 94.0°C.",
            ),
        ).toBeInTheDocument();
    });

    it("paginates historical alerts independently from active alerts", async () => {
        const user =
            userEvent.setup({
                advanceTimers:
                jest.advanceTimersByTime,
            });

        renderAlerts({
            active: createAlerts(
                6,
                "active",
            ),
            historical: createAlerts(
                11,
                "historical",
            ),
        });

        const activeSection =
            getActiveSection();

        const historySection =
            getHistorySection();

        expect(
            within(activeSection).getByText(
                "GPU Node 1",
            ),
        ).toBeInTheDocument();

        expect(
            within(activeSection).getByText(
                "GPU Node 5",
            ),
        ).toBeInTheDocument();

        expect(
            within(activeSection).queryByText(
                "GPU Node 6",
            ),
        ).not.toBeInTheDocument();

        const activeNextButton =
            within(activeSection).getByRole(
                "button",
                {
                    name: /next/i,
                },
            );

        const historyNextButton =
            within(historySection).getByRole(
                "button",
                {
                    name: /next/i,
                },
            );

        expect(
            activeNextButton,
        ).toBeInTheDocument();

        expect(
            historyNextButton,
        ).toBeInTheDocument();

        await user.click(
            activeNextButton,
        );

        expect(
            within(activeSection).getByText(
                "GPU Node 6",
            ),
        ).toBeInTheDocument();

        expect(
            within(historySection).getByText(
                "Historical Node 10",
            ),
        ).toBeInTheDocument();

        expect(
            within(historySection).queryByText(
                "Historical Node 11",
            ),
        ).not.toBeInTheDocument();
    });

    it("keeps a recently cleared active alert visible", () => {
        const activeAlert =
            createAlert({
                id: "cleared-alert",
                nodeId: "node-1",
                nodeName: "GPU Node 1",
            });

        const { rerender } =
            renderAlerts({
                active: [activeAlert],
            });

        const activeSection =
            getActiveSection();

        expect(
            within(activeSection).getByText(
                "GPU Node 1",
            ),
        ).toBeInTheDocument();

        clearActiveAlerts(rerender);

        /*
         * The alert should still be rendered
         * immediately after becoming inactive.
         */
        expect(
            within(activeSection).getByText(
                "GPU Node 1",
            ),
        ).toBeInTheDocument();

        expect(
            within(activeSection).getByText(
                "Cleared within the last 5 minutes",
            ),
        ).toBeInTheDocument();
    });

    it("removes a recently cleared alert after five minutes", () => {
        const activeAlert =
            createAlert({
                id: "cleared-alert",
                nodeId: "node-1",
                nodeName: "GPU Node 1",
            });

        const { rerender } =
            renderAlerts({
                active: [activeAlert],
            });

        expect(
            screen.getByText(
                "GPU Node 1",
            ),
        ).toBeInTheDocument();

        clearActiveAlerts(rerender);

        expect(
            screen.getByText(
                "GPU Node 1",
            ),
        ).toBeInTheDocument();

        expect(
            screen.getByText(
                "Cleared within the last 5 minutes",
            ),
        ).toBeInTheDocument();

        act(() => {
            jest.advanceTimersByTime(
                5 * 60 * 1000,
            );
        });

        expect(
            screen.queryByText(
                "GPU Node 1",
            ),
        ).not.toBeInTheDocument();

        expect(
            screen.getByText(
                "No active health alerts.",
            ),
        ).toBeInTheDocument();
    });

    it("does not extend cleared-alert retention when polling continues", () => {
        const activeAlert =
            createAlert({
                id: "cleared-alert",
                nodeId: "node-1",
                nodeName: "GPU Node 1",
            });

        const { rerender } =
            renderAlerts({
                active: [activeAlert],
            });

        clearActiveAlerts(rerender);

        act(() => {
            jest.advanceTimersByTime(
                30_000,
            );
        });

        /*
         * Simulate another parent render while
         * the alert remains cleared.
         */
        act(() => {
            rerender(
                <HealthAlerts
                    alerts={{
                        active: [],
                        historical: [],
                    }}
                />,
            );
        });

        act(() => {
            jest.advanceTimersByTime(
                60_000,
            );
        });

        act(() => {
            rerender(
                <HealthAlerts
                    alerts={{
                        active: [],
                        historical: [],
                    }}
                />,
            );
        });

        /*
         * Five minutes from the original clear
         * point have now elapsed.
         */
        act(() => {
            jest.advanceTimersByTime(
                4 * 60 * 1000,
            );
        });

        expect(
            screen.queryByText(
                "GPU Node 1",
            ),
        ).not.toBeInTheDocument();
    });

    it("shows active alerts before recently cleared alerts", () => {
        const clearedAlert =
            createAlert({
                id: "cleared-alert",
                nodeId: "node-cleared",
                nodeName: "Cleared Node",
            });

        const activeAlert =
            createAlert({
                id: "active-alert",
                nodeId: "node-active",
                nodeName: "Active Node",
            });

        const { rerender } =
            renderAlerts({
                active: [clearedAlert],
            });

        clearActiveAlerts(rerender);

        act(() => {
            rerender(
                <HealthAlerts
                    alerts={{
                        active: [
                            activeAlert,
                        ],
                        historical: [],
                    }}
                />,
            );
        });

        const activeSection =
            getActiveSection();

        const nodeLinks =
            within(
                activeSection,
            ).getAllByRole("link");

        expect(
            nodeLinks[0],
        ).toHaveTextContent(
            "Active Node",
        );

        expect(
            nodeLinks[1],
        ).toHaveTextContent(
            "Cleared Node",
        );
    });

    it("updates an existing active alert instead of creating a duplicate", () => {
        const firstAlert =
            createAlert({
                id: "health-1",
                nodeId: "node-1",
                nodeName: "GPU Node 1",
                message:
                    "CPU usage is high (91.0%).",
            });

        const updatedAlert =
            createAlert({
                id: "health-2",
                nodeId: "node-1",
                nodeName: "GPU Node 1",
                message:
                    "CPU usage is high (97.0%).",
            });

        const { rerender } =
            renderAlerts({
                active: [firstAlert],
            });

        act(() => {
            rerender(
                <HealthAlerts
                    alerts={{
                        active: [
                            updatedAlert,
                        ],
                        historical: [],
                    }}
                />,
            );
        });

        expect(
            screen.getByText(
                "CPU usage is high (97.0%).",
            ),
        ).toBeInTheDocument();

        expect(
            screen.queryByText(
                "CPU usage is high (91.0%).",
            ),
        ).not.toBeInTheDocument();

        expect(
            screen.getAllByText(
                "GPU Node 1",
            ),
        ).toHaveLength(1);
    });

    it("resets active-alert pagination when the alert collection changes", async () => {
        const user =
            userEvent.setup({
                advanceTimers:
                jest.advanceTimersByTime,
            });

        const firstAlerts =
            createAlerts(7);

        const { rerender } =
            renderAlerts({
                active: firstAlerts,
            });

        const activeSection =
            getActiveSection();

        await user.click(
            within(activeSection).getByRole(
                "button",
                {
                    name: /next/i,
                },
            ),
        );

        expect(
            within(activeSection).getByText(
                "GPU Node 6",
            ),
        ).toBeInTheDocument();

        const updatedAlerts =
            createAlerts(
                6,
                "updated",
            );

        act(() => {
            rerender(
                <HealthAlerts
                    alerts={{
                        active:
                        updatedAlerts,
                        historical: [],
                    }}
                />,
            );
        });

        expect(
            within(activeSection).getByText(
                "GPU Node 1",
            ),
        ).toBeInTheDocument();

        expect(
            within(activeSection).queryByText(
                "GPU Node 6",
            ),
        ).not.toBeInTheDocument();
    });
});