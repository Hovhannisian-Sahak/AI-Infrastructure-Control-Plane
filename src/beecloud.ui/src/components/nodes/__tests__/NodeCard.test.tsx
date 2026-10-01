import { render, screen } from "@testing-library/react";
import NodeCard from "../NodeCard";
import type { ComputeNode } from "@/lib/api/models/computeNode";

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
        render(<NodeCard node={node} />);

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
        const { rerender } = render(<NodeCard node={node} />);

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
});