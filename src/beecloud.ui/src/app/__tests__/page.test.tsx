import { render, screen } from "@testing-library/react";
import Home from "../page";

describe("Home page", () => {
    it("renders the main heading", () => {
        render(<Home />);

        expect(
            screen.getByRole("heading", {
                name: /to get started, edit the page\.tsx file/i,
            }),
        ).toBeInTheDocument();
    });

    it("renders the expected links", () => {
        render(<Home />);

        expect(screen.getByRole("link", { name: "Templates" })).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Learning" })).toBeInTheDocument();
        expect(screen.getByRole("link", { name: /Deploy Now/ }),).toBeInTheDocument();
    });
});