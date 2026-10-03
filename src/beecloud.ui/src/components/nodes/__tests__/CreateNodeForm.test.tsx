import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CreateNodeForm from "../CreateNodeForm";

describe("CreateNodeForm", () => {
  it("renders all fields", () => {
    render(<CreateNodeForm isSubmitting={false} onSubmit={jest.fn()} />);

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

    render(<CreateNodeForm isSubmitting={false} onSubmit={onSubmit} />);

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
    render(<CreateNodeForm isSubmitting={false} onSubmit={jest.fn()} />);

    expect(
      screen.getByRole("spinbutton", {
        name: "GPU Count",
      }),
    ).toHaveValue(1);
  });
  it("disables the submit button while creating a node", () => {
    render(<CreateNodeForm onSubmit={jest.fn()} isSubmitting={true} />);

    expect(
      screen.getByRole("button", {
        name: "Creating node...",
      }),
    ).toBeDisabled();
  });
  it("shows an error when the GPU model is empty", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

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

    expect(screen.getByRole("alert")).toHaveTextContent("GPU model is required.");

    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("shows an error when the GPU count is zero", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

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

    expect(screen.getByRole("alert")).toHaveTextContent("GPU count must be greater than zero.");

    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("clears the node name error when the user corrects the field", async () => {
    const user = userEvent.setup();

    render(<CreateNodeForm onSubmit={jest.fn()} isSubmitting={false} />);

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("Node name is required.");

    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );

    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
  it("renders the create node heading", () => {
    render(<CreateNodeForm onSubmit={jest.fn()} isSubmitting={false} />);

    expect(
      screen.getByRole("heading", {
        name: "Create Compute Node",
      }),
    ).toBeInTheDocument();
  });
  it("submits the complete node data", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
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
      name: "GPU Node 1",
      gpuModel: "NVIDIA H100",
      gpuCount: 8,
    });
  });
  it("shows the submitting state", () => {
    render(<CreateNodeForm onSubmit={jest.fn()} isSubmitting={true} />);

    const button = screen.getByRole("button", {
      name: "Creating node...",
    });

    expect(button).toBeDisabled();
  });
  it("shows an error when the GPU count is zero", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    const gpuCount = screen.getByRole("spinbutton", {
      name: "GPU Count",
    });
    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );

    await user.type(
      screen.getByRole("textbox", {
        name: "GPU Model",
      }),
      "NVIDIA H100",
    );
    await user.clear(gpuCount);
    await user.type(gpuCount, "0");

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("GPU count must be greater than zero.");

    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("shows an error when the GPU model is empty", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);
    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );
    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("GPU model is required.");

    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("shows an error when the node name contains only whitespace", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "   ",
    );
    await user.type(
      screen.getByRole("textbox", {
        name: "GPU Model",
      }),
      "NVIDIA H100",
    );
    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("Node name is required.");

    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("shows an error when the GPU model contains only whitespace", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    await user.type(
      screen.getByRole("textbox", {
        name: "GPU Model",
      }),
      "   ",
    );
    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("GPU model is required.");

    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("shows an error when the GPU count is negative", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    const gpuCount = screen.getByRole("spinbutton", {
      name: "GPU Count",
    });
    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );

    await user.type(
      screen.getByRole("textbox", {
        name: "GPU Model",
      }),
      "NVIDIA H100",
    );
    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );

    await user.type(
      screen.getByRole("textbox", {
        name: "GPU Model",
      }),
      "NVIDIA H100",
    );
    await user.clear(gpuCount);
    await user.type(gpuCount, "-2");

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("GPU count must be greater than zero.");

    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("shows an error when the GPU count is not an integer", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    const gpuCount = screen.getByRole("spinbutton", {
      name: "GPU Count",
    });
    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );

    await user.type(
      screen.getByRole("textbox", {
        name: "GPU Model",
      }),
      "NVIDIA H100",
    );
    await user.clear(gpuCount);
    await user.type(gpuCount, "2.5");

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("GPU count must be greater than zero.");

    expect(onSubmit).not.toHaveBeenCalled();
  });
  it("clears the node name error after entering a valid name", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    const nameInput = screen.getByRole("textbox", {
      name: "Node Name",
    });

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("Node name is required.");

    await user.type(nameInput, "GPU Node 1");

    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
  it("clears the GPU model error after entering a valid model", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    const gpuModelInput = screen.getByRole("textbox", {
      name: "GPU Model",
    });
    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );

    await user.type(
      screen.getByRole("textbox", {
        name: "GPU Model",
      }),
      "NVIDIA H100",
    );
    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    await user.type(gpuModelInput, "NVIDIA H100");

    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
  it("clears the GPU count error after entering a valid count", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(<CreateNodeForm onSubmit={onSubmit} isSubmitting={false} />);

    const gpuCount = screen.getByRole("spinbutton", {
      name: "GPU Count",
    });
    await user.type(
      screen.getByRole("textbox", {
        name: "Node Name",
      }),
      "GPU Node 1",
    );

    await user.type(
      screen.getByRole("textbox", {
        name: "GPU Model",
      }),
      "NVIDIA H100",
    );
    await user.clear(gpuCount);
    await user.type(gpuCount, "0");

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(screen.getByRole("alert")).toHaveTextContent("GPU count must be greater than zero.");

    await user.clear(gpuCount);
    await user.type(gpuCount, "8");

    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
