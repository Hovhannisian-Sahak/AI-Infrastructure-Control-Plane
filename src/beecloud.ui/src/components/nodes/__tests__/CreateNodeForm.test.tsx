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
    it("disables the submit button while creating a node", () => {
        render(
            <CreateNodeForm
                onSubmit={jest.fn()}
                isSubmitting={true}
            />,
        );

        expect(
            screen.getByRole("button", {
                name: "Creating node...",
            }),
        ).toBeDisabled();
    });
    it("shows an error when the GPU model is empty", async () => {
        const user = userEvent.setup();
        const onSubmit = jest.fn();

        render(
            <CreateNodeForm
                onSubmit={onSubmit}
                isSubmitting={false}
            />,
        );

        await user.type(
            screen.getByRole("textbox", {
                name: "Node Name",
            }),
            "GPU Node 2",
        );

        await user.click(
            screen.getByRole("button", {
                name: "Create Node",
            }),
        );

        expect(
            screen.getByRole("alert"),
        ).toHaveTextContent("GPU model is required.");

        expect(onSubmit).not.toHaveBeenCalled();
    });
    it("shows an error when the GPU count is zero", async () => {
        const user = userEvent.setup();
        const onSubmit = jest.fn();

        render(
            <CreateNodeForm
                onSubmit={onSubmit}
                isSubmitting={false}
            />,
        );

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
        await user.type(gpuCount, "0");

        await user.click(
            screen.getByRole("button", {
                name: "Create Node",
            }),
        );

        expect(
            screen.getByRole("alert"),
        ).toHaveTextContent(
            "GPU count must be greater than zero.",
        );

        expect(onSubmit).not.toHaveBeenCalled();
    });
    it("clears the node name error when the user corrects the field", async () => {
        const user = userEvent.setup();

        render(
            <CreateNodeForm
                onSubmit={jest.fn()}
                isSubmitting={false}
            />,
        );

        await user.click(
            screen.getByRole("button", {
                name: "Create Node",
            }),
        );

        expect(
            screen.getByRole("alert"),
        ).toHaveTextContent("Node name is required.");

        await user.type(
            screen.getByRole("textbox", {
                name: "Node Name",
            }),
            "GPU Node 1",
        );

        expect(
            screen.queryByRole("alert"),
        ).not.toBeInTheDocument();
    });
});