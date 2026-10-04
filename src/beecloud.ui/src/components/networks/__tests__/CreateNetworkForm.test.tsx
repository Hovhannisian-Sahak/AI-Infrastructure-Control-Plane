import {
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CreateNetworkForm from "../CreateNetworkForm";

describe("CreateNetworkForm", () => {
  const mockOnSubmit = jest
    .fn()
    .mockResolvedValue(undefined);

  beforeEach(() => {
    mockOnSubmit.mockClear();
  });

  it("renders the form with an empty submit button disabled", () => {
    const onSubmit = jest.fn();

    render(
      <CreateNetworkForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    expect(
      screen.getByRole("heading", {
        name: "Create Network",
      }),
    ).toBeInTheDocument();

    expect(
      screen.getByLabelText("Network name"),
    ).toHaveValue("");

    expect(
      screen.getByLabelText(/Description/),
    ).toHaveValue("");

    expect(
      screen.getByRole("button", {
        name: "Create Network",
      }),
    ).toBeDisabled();
  });

  it("submits a trimmed network name and description", async () => {
    const user = userEvent.setup();
    const onSubmit = jest
      .fn()
      .mockResolvedValue(undefined);

    render(
      <CreateNetworkForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Network name"),
      "  gpu-production  ",
    );

    await user.type(
      screen.getByLabelText(/Description/),
      "  Production GPU network  ",
    );

    await user.click(
      screen.getByRole("button", {
        name: "Create Network",
      }),
    );

    expect(onSubmit).toHaveBeenCalledWith({
      name: "gpu-production",
      description: "Production GPU network",
    });
  });

  it("shows a validation error when the name is only whitespace", () => {
    render(
      <CreateNetworkForm
        onSubmit={mockOnSubmit}
        isSubmitting={false}
      />,
    );

    const nameInput =
      screen.getByLabelText("Network name");

    fireEvent.change(nameInput, {
      target: {
        value: "   ",
      },
    });

    const form = nameInput.closest("form");

    fireEvent.submit(form!);

    expect(
      screen.getByText(
        "Network name is required.",
      ),
    ).toBeInTheDocument();

    expect(
      mockOnSubmit,
    ).not.toHaveBeenCalled();
  });

  it("limits the network name to 100 characters", () => {
    render(
        <CreateNetworkForm
            onSubmit={mockOnSubmit}
            isSubmitting={false}
        />,
    );

    expect(
        screen.getByLabelText("Network name"),
    ).toHaveAttribute("maxLength", "100");
  });

  it("limits the description to 500 characters", () => {
    render(
        <CreateNetworkForm
            onSubmit={mockOnSubmit}
            isSubmitting={false}
        />,
    );

    expect(
        screen.getByLabelText(/Description/),
    ).toHaveAttribute("maxLength", "500");
  });

  it("submits undefined description when description is empty", async () => {
    const user = userEvent.setup();
    const onSubmit = jest
      .fn()
      .mockResolvedValue(undefined);

    render(
      <CreateNetworkForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    await user.type(
      screen.getByLabelText("Network name"),
      "gpu-production",
    );

    await user.click(
      screen.getByRole("button", {
        name: "Create Network",
      }),
    );

    expect(onSubmit).toHaveBeenCalledWith({
      name: "gpu-production",
      description: undefined,
    });
  });

  it("clears the form after successful submission", async () => {
    const user = userEvent.setup();
    const onSubmit = jest
      .fn()
      .mockResolvedValue(undefined);

    render(
      <CreateNetworkForm
        onSubmit={onSubmit}
        isSubmitting={false}
      />,
    );

    const nameInput =
      screen.getByLabelText("Network name");

    const descriptionInput =
      screen.getByLabelText(/Description/);

    await user.type(
      nameInput,
      "gpu-production",
    );

    await user.type(
      descriptionInput,
      "Production network",
    );

    await user.click(
      screen.getByRole("button", {
        name: "Create Network",
      }),
    );

    expect(nameInput).toHaveValue("");
    expect(descriptionInput).toHaveValue("");
  });

  it("shows creating state and disables the form while submitting", () => {
    const onSubmit = jest.fn();

    render(
      <CreateNetworkForm
        onSubmit={onSubmit}
        isSubmitting={true}
      />,
    );

    expect(
      screen.getByRole("button", {
        name: "Creating...",
      }),
    ).toBeDisabled();
  });

  it("clears the validation error when the name is changed", () => {
    render(
      <CreateNetworkForm
        onSubmit={mockOnSubmit}
        isSubmitting={false}
      />,
    );

    const nameInput =
      screen.getByLabelText("Network name");

    const form = nameInput.closest("form");

    fireEvent.submit(form!);

    expect(
      screen.getByText(
        "Network name is required.",
      ),
    ).toBeInTheDocument();

    fireEvent.change(nameInput, {
      target: {
        value: "gpu-production",
      },
    });

    expect(
      screen.queryByText(
        "Network name is required.",
      ),
    ).not.toBeInTheDocument();
  });
});
