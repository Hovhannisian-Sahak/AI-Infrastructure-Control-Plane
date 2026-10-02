import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CreateNodeForm from "../CreateNodeForm";

describe("CreateNodeForm", () => {
    it("renders all fields", () => {
        render(<CreateNodeForm onSubmit={jest.fn()} />);

        expect(
            screen.getByRole("textbox", {
                name: "Node Name",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByRole("textbox", {
                name: "GPU Model",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByRole("spinbutton", {
                name: "GPU Count",
            }),
        ).toBeInTheDocument();

        expect(
            screen.getByRole("button", {
                name: "Create Node",
            }),
        ).toBeInTheDocument();
    });

    it("submits the entered node data", async () => {
        const user = userEvent.setup();
        const onSubmit = jest.fn();

        render(<CreateNodeForm onSubmit={onSubmit} />);

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

        const gpuCount = screen.getByRole("spinbutton", {
            name: "GPU Count",
        });

        await user.clear(gpuCount);
        await user.type(gpuCount, "8");

        await user.click(
            screen.getByRole("button", {
                name: "Create Node",
            }),
        );

        expect(onSubmit).toHaveBeenCalledWith({
            name: "GPU Node 2",
            gpuModel: "NVIDIA H100",
            gpuCount: 8,
        });
    });

    it("uses one GPU by default", () => {
        render(<CreateNodeForm onSubmit={jest.fn()} />);

        expect(
            screen.getByRole("spinbutton", {
                name: "GPU Count",
            }),
        ).toHaveValue(1);
    });
});