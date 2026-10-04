import {
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CreateNodeForm from "../CreateNodeForm";

describe("CreateNodeForm", () => {
  it("renders all fields", () => {
    render(
      <CreateNodeForm
        onSubmit={jest.fn()}
        isSubmitting={false}
      />,
    );

    expect(
      screen.getByLabelText("Node Name"),
    ).toBeInTheDocument();

    expect(
      screen.getByLabelText("GPU Model"),
    ).toBeInTheDocument();

    expect(
      screen.getByLabelText("GPU Count"),
    ).toBeInTheDocument();

    expect(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    ).toBeInTheDocument();
  });

  it("renders the create node heading", () => {
    render(
      <CreateNodeForm
        onSubmit={jest.fn()}
        isSubmitting={false}
      />,
    );

    expect(
      screen.getByRole("heading", {
        name: "Create Compute Node",
      }),
    ).toBeInTheDocument();
  });

  it("uses one GPU by default", () => {
    render(
      <CreateNodeForm
        onSubmit={jest.fn()}
        isSubmitting={false}
      />,
    );

    expect(
      screen.getByLabelText("GPU Count"),
    ).toHaveValue(1);
  });

  it("submits the entered node data", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "GPU Node 2",
    );

    await user.type(
      screen.getByLabelText("GPU Model"),
      "NVIDIA H100",
    );

    const gpuCount = screen.getByLabelText("GPU Count");

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

  it("shows an error when the node name is empty", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
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
    ).toHaveTextContent(
      "Node name is required.",
    );

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("shows an error when the node name contains only whitespace", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "   ",
    );

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(
      screen.getByRole("alert"),
    ).toHaveTextContent(
      "Node name is required.",
    );

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("shows an error when node name exceeds 100 characters", () => {
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    fireEvent.change(
      screen.getByLabelText("Node Name"),
      {
        target: {
          value: "a".repeat(101),
        },
      },
    );

    fireEvent.change(
      screen.getByLabelText("GPU Model"),
      {
        target: {
          value: "NVIDIA A100",
        },
      },
    );

    fireEvent.submit(
      screen
        .getByRole("button", {
          name: "Create Node",
        })
        .closest("form")!,
    );

    expect(
      screen.getByText(
        "Node name must be 100 characters or fewer.",
      ),
    ).toBeInTheDocument();

    expect(onSubmit).not.toHaveBeenCalled();
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
      screen.getByLabelText("Node Name"),
      "GPU Node 1",
    );

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(
      screen.getByRole("alert"),
    ).toHaveTextContent(
      "GPU model is required.",
    );

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("shows an error when the GPU model contains only whitespace", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "GPU Node 1",
    );

    await user.type(
      screen.getByLabelText("GPU Model"),
      "   ",
    );

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(
      screen.getByRole("alert"),
    ).toHaveTextContent(
      "GPU model is required.",
    );

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("shows an error when GPU model exceeds 100 characters", () => {
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    fireEvent.change(
      screen.getByLabelText("Node Name"),
      {
        target: {
          value: "gpu-node-01",
        },
      },
    );

    fireEvent.change(
      screen.getByLabelText("GPU Model"),
      {
        target: {
          value: "a".repeat(101),
        },
      },
    );

    fireEvent.submit(
      screen
        .getByRole("button", {
          name: "Create Node",
        })
        .closest("form")!,
    );

    expect(
      screen.getByText(
        "GPU model must be 100 characters or fewer.",
      ),
    ).toBeInTheDocument();

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("shows an error when GPU count is zero", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "GPU Node 1",
    );

    await user.type(
      screen.getByLabelText("GPU Model"),
      "NVIDIA H100",
    );

    const gpuCount = screen.getByLabelText("GPU Count");

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
      "GPU count must be between 1 and 16.",
    );

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("shows an error when GPU count is negative", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "GPU Node 1",
    );

    await user.type(
      screen.getByLabelText("GPU Model"),
      "NVIDIA H100",
    );

    const gpuCount = screen.getByLabelText("GPU Count");

    await user.clear(gpuCount);
    await user.type(gpuCount, "-2");

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(
      screen.getByRole("alert"),
    ).toHaveTextContent(
      "GPU count must be between 1 and 16.",
    );

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("shows an error when GPU count is greater than 16", () => {
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    fireEvent.change(
      screen.getByLabelText("Node Name"),
      {
        target: {
          value: "gpu-node-01",
        },
      },
    );

    fireEvent.change(
      screen.getByLabelText("GPU Model"),
      {
        target: {
          value: "NVIDIA A100",
        },
      },
    );

    fireEvent.change(
      screen.getByLabelText("GPU Count"),
      {
        target: {
          value: "17",
        },
      },
    );

    fireEvent.submit(
      screen
        .getByRole("button", {
          name: "Create Node",
        })
        .closest("form")!,
    );

    expect(
      screen.getByText(
        "GPU count must be between 1 and 16.",
      ),
    ).toBeInTheDocument();

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("allows GPU count of 16", () => {
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    fireEvent.change(
      screen.getByLabelText("Node Name"),
      {
        target: {
          value: "gpu-node-01",
        },
      },
    );

    fireEvent.change(
      screen.getByLabelText("GPU Model"),
      {
        target: {
          value: "NVIDIA A100",
        },
      },
    );

    fireEvent.change(
      screen.getByLabelText("GPU Count"),
      {
        target: {
          value: "16",
        },
      },
    );

    fireEvent.submit(
      screen
        .getByRole("button", {
          name: "Create Node",
        })
        .closest("form")!,
    );

    expect(onSubmit).toHaveBeenCalledWith({
      name: "gpu-node-01",
      gpuModel: "NVIDIA A100",
      gpuCount: 16,
    });
  });

  it("shows an error when GPU count is not an integer", async () => {
    const user = userEvent.setup();
    const onSubmit = jest.fn();

    render(
      <CreateNodeForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "GPU Node 1",
    );

    await user.type(
      screen.getByLabelText("GPU Model"),
      "NVIDIA H100",
    );

    const gpuCount = screen.getByLabelText("GPU Count");

    await user.clear(gpuCount);
    await user.type(gpuCount, "2.5");

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(
      screen.getByRole("alert"),
    ).toHaveTextContent(
      "GPU count must be between 1 and 16.",
    );

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("clears the node name error after entering a valid name", async () => {
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
    ).toHaveTextContent(
      "Node name is required.",
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "GPU Node 1",
    );

    expect(
      screen.queryByRole("alert"),
    ).not.toBeInTheDocument();
  });

  it("clears the GPU model error after entering a valid model", async () => {
    const user = userEvent.setup();

    render(
      <CreateNodeForm
        onSubmit={jest.fn()}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "GPU Node 1",
    );

    await user.click(
      screen.getByRole("button", {
        name: "Create Node",
      }),
    );

    expect(
      screen.getByRole("alert"),
    ).toHaveTextContent(
      "GPU model is required.",
    );

    await user.type(
      screen.getByLabelText("GPU Model"),
      "NVIDIA H100",
    );

    expect(
      screen.queryByRole("alert"),
    ).not.toBeInTheDocument();
  });

  it("clears the GPU count error after entering a valid count", async () => {
    const user = userEvent.setup();

    render(
      <CreateNodeForm
        onSubmit={jest.fn()}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Node Name"),
      "GPU Node 1",
    );

    await user.type(
      screen.getByLabelText("GPU Model"),
      "NVIDIA H100",
    );

    const gpuCount = screen.getByLabelText("GPU Count");

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
      "GPU count must be between 1 and 16.",
    );

    await user.clear(gpuCount);
    await user.type(gpuCount, "8");

    expect(
      screen.queryByRole("alert"),
    ).not.toBeInTheDocument();
  });
});