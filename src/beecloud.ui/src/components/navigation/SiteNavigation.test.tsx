import { render, screen } from "@testing-library/react";
import { usePathname } from "next/navigation";

import SiteNavigation from "./SiteNavigation";

jest.mock("next/navigation", () => ({
    usePathname: jest.fn(),
}));

const mockedUsePathname = jest.mocked(usePathname);

describe("SiteNavigation", () => {
    it("links to Nodes and Health and marks the current page", () => {
        mockedUsePathname.mockReturnValue("/health");

        render(<SiteNavigation />);

        expect(
            screen.getByRole("link", { name: "BeeCloud" }),
        ).toHaveAttribute("href", "/");
        expect(
            screen.getByRole("link", { name: "Nodes" }),
        ).toHaveAttribute("href", "/");
        expect(
            screen.getByRole("link", { name: "Health" }),
        ).toHaveAttribute("href", "/health");
        expect(
            screen.getByRole("link", { name: "Health" }),
        ).toHaveAttribute("aria-current", "page");
    });

    it("marks Nodes active on node detail routes", () => {
        mockedUsePathname.mockReturnValue("/nodes/node-1");

        render(<SiteNavigation />);

        expect(
            screen.getByRole("link", { name: "Nodes" }),
        ).toHaveAttribute("aria-current", "page");
    });
});
