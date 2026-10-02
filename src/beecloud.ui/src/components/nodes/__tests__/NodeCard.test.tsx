import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "@/test-utils";
import NodeCard from "../NodeCard";
import type { ComputeNode } from "@/lib/api/models/computeNode";
import { nodesApi } from "@/lib/api/nodesApi";
import { store } from "@/store/store";

jest.mock("@/lib/api/nodesApi");

const mockedNodesApi = jest.mocked(nodesApi);

const node: ComputeNode = {
    id: "node-1",
    name: "GPU Node 1",
    gpuModel: "NVIDIA A100",
    gpuCount: 4,
    status: "Available",
    activeFault: "None",
};

describe("NodeCard", () => {
    it("renders node information", () => {
        renderWithProviders(<NodeCard node={node} />);

        expect(
            screen.getByRole("heading", { name: "GPU Node 1" }),
        ).toBeInTheDocument();

        const gpuModel = screen.getByText("GPU Model").parentElement;
        expect(gpuModel).toHaveTextContent("NVIDIA A100");

        const gpuCount = screen.getByText("GPU Count").parentElement;
        expect(gpuCount).toHaveTextContent("4");

        const activeFault = screen.getByText("Active Fault").parentElement;
        expect(activeFault).toHaveTextContent("None");

        expect(
            screen.getByText("Available"),
        ).toBeInTheDocument();
    });
    it("applies the correct status style", () => {
        const { rerender } = renderWithProviders(<NodeCard node={node} />);

        expect(screen.getByText("Available")).toBeInTheDocument();

        rerender(
            <NodeCard
                node={{
                    ...node,
                    status: "Failed",
                }}
            />,
        );
        
        expect(screen.getByText("Failed")).toBeInTheDocument();
    });
    it("starts an available node when Start is clicked", async () => {
        const user = userEvent.setup();

        mockedNodesApi.start.mockResolvedValue({
            ...node,
            status: "Running",
        });

        store.dispatch({
            type: "nodes/fetchNodes/fulfilled",
            payload: [node],
        });

        renderWithProviders(<NodeCard node={node} />);

        const startButton = screen.getByRole("button", {
            name: "Start",
        });

        expect(startButton).toBeInTheDocument();
        expect(
            screen.queryByRole("button", { name: "Stop" }),
        ).not.toBeInTheDocument();

        await user.click(startButton);

        expect(mockedNodesApi.start).toHaveBeenCalledWith("node-1");

        expect(store.getState().nodes.nodes).toEqual([
            {
                ...node,
                status: "Running",
            },
        ]);
    });
    it("stops a running node when Stop is clicked", async () => {
        const user = userEvent.setup();

        const runningNode: ComputeNode = {
            ...node,
            status: "Running",
        };

        mockedNodesApi.stop.mockResolvedValue({
            ...runningNode,
            status: "Stopped",
        });

        store.dispatch({
            type: "nodes/fetchNodes/fulfilled",
            payload: [runningNode],
        });

        renderWithProviders(<NodeCard node={runningNode} />);

        const stopButton = screen.getByRole("button", {
            name: "Stop",
        });

        expect(stopButton).toBeInTheDocument();
        expect(
            screen.queryByRole("button", { name: "Start" }),
        ).not.toBeInTheDocument();

        await user.click(stopButton);

        expect(mockedNodesApi.stop).toHaveBeenCalledWith("node-1");

        expect(store.getState().nodes.nodes).toEqual([
            {
                ...runningNode,
                status: "Stopped",
            },
        ]);
    });
    it("does not show actions for an unhealthy node", () => {
        const unhealthyNode: ComputeNode = {
            ...node,
            status: "Unhealthy",
        };

        renderWithProviders(
            <NodeCard node={unhealthyNode} />,
        );

        expect(
            screen.queryByRole("button", { name: "Start" }),
        ).not.toBeInTheDocument();

        expect(
            screen.queryByRole("button", { name: "Stop" }),
        ).not.toBeInTheDocument();
    });
    it("shows Start and hides Stop for a stopped node", () => {
        const stoppedNode: ComputeNode = {
            ...node,
            status: "Stopped",
        };

        renderWithProviders(
            <NodeCard node={stoppedNode} />,
        );

        expect(
            screen.getByRole("button", { name: "Start" }),
        ).toBeInTheDocument();

        expect(
            screen.queryByRole("button", { name: "Stop" }),
        ).not.toBeInTheDocument();
    });
    it("keeps the node unchanged when starting fails", async () => {
        const user = userEvent.setup();

        mockedNodesApi.start.mockRejectedValue(
            new Error("Failed to start node"),
        );

        store.dispatch({
            type: "nodes/fetchNodes/fulfilled",
            payload: [node],
        });

        renderWithProviders(<NodeCard node={node} />);

        await user.click(
            screen.getByRole("button", { name: "Start" }),
        );

        expect(mockedNodesApi.start).toHaveBeenCalledWith("node-1");

        expect(store.getState().nodes.nodes).toEqual([node]);
    });
    it("keeps the node unchanged when stopping fails", async () => {
        const user = userEvent.setup();

        const runningNode: ComputeNode = {
            ...node,
            status: "Running",
        };

        mockedNodesApi.stop.mockRejectedValue(
            new Error("Failed to stop node"),
        );

        store.dispatch({
            type: "nodes/fetchNodes/fulfilled",
            payload: [runningNode],
        });

        renderWithProviders(
            <NodeCard node={runningNode} />,
        );

        await user.click(
            screen.getByRole("button", { name: "Stop" }),
        );

        expect(mockedNodesApi.stop).toHaveBeenCalledWith("node-1");

        expect(store.getState().nodes.nodes).toEqual([
            runningNode,
        ]);
    });
    it("shows Starting while a node is being started", async () => {
        const user = userEvent.setup();

        let resolveStart: (
            value: ComputeNode,
        ) => void;

        const startPromise = new Promise<ComputeNode>((resolve) => {
            resolveStart = resolve;
        });

        mockedNodesApi.start.mockReturnValue(startPromise);

        store.dispatch({
            type: "nodes/fetchNodes/fulfilled",
            payload: [node],
        });

        renderWithProviders(<NodeCard node={node} />);

        const startButton = screen.getByRole("button", {
            name: "Start",
        });

        await user.click(startButton);

        expect(
            screen.getByRole("button", {
                name: "Starting...",
            }),
        ).toBeDisabled();

        resolveStart!({
            ...node,
            status: "Running",
        });

        await startPromise;
    });
    it("shows Stopping while a node is being stopped", async () => {
        const user = userEvent.setup();

        const runningNode: ComputeNode = {
            ...node,
            status: "Running",
        };

        let resolveStop: (
            value: ComputeNode,
        ) => void;

        const stopPromise = new Promise<ComputeNode>((resolve) => {
            resolveStop = resolve;
        });

        mockedNodesApi.stop.mockReturnValue(stopPromise);

        store.dispatch({
            type: "nodes/fetchNodes/fulfilled",
            payload: [runningNode],
        });

        renderWithProviders(
            <NodeCard node={runningNode} />,
        );

        const stopButton = screen.getByRole("button", {
            name: "Stop",
        });

        await user.click(stopButton);

        expect(
            screen.getByRole("button", {
                name: "Stopping...",
            }),
        ).toBeDisabled();

        resolveStop!({
            ...runningNode,
            status: "Stopped",
        });

        await stopPromise;
    });
    it("applies the correct status style", () => {
        const { rerender } = renderWithProviders(<NodeCard node={node} />);

        expect(screen.getByText("Available")).toBeInTheDocument();

        rerender(
            <NodeCard
                node={{
                    ...node,
                    status: "Failed",
                }}
            />,
        );

        expect(screen.getByText("Failed")).toBeInTheDocument();
    });

    it("shows a provisioning message and hides actions for a provisioning node", () => {
        const provisioningNode: ComputeNode = {
            ...node,
            status: "Provisioning",
        };

        renderWithProviders(
            <NodeCard node={provisioningNode} />,
        );

        const provisioningMessage = screen.getByText(
            "Node is being provisioned...",
        );

        expect(provisioningMessage).toBeInTheDocument();
        expect(provisioningMessage).toHaveClass(
            "info",
        );

        expect(
            screen.queryByRole("button", { name: "Start" }),
        ).not.toBeInTheDocument();

        expect(
            screen.queryByRole("button", { name: "Stop" }),
        ).not.toBeInTheDocument();
    });
});