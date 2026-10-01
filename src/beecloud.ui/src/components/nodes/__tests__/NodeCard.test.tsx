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

        expect(
            screen.getByText("GPU: NVIDIA A100 × 4"),
        ).toBeInTheDocument();

        expect(screen.getByText("Status: Available")).toBeInTheDocument();

        expect(screen.getByText("Fault: None")).toBeInTheDocument();
    });
});