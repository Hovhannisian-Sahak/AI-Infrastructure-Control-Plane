import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import Pagination from "../Pagination";

function PaginatedContent() {
    const [page, setPage] = useState(1);
    return (
        <>
            <p>Rows for page {page}</p>
            <Pagination
                page={page}
                pageSize={10}
                totalItems={20}
                ariaLabel="Test pages"
                onPageChange={setPage}
            />
        </>
    );
}

describe("Pagination", () => {
    it("restores the viewport position after changing pages", () => {
        const scrollTo = jest.spyOn(window, "scrollTo").mockImplementation(() => {});
        const requestAnimationFrame = jest
            .spyOn(window, "requestAnimationFrame")
            .mockImplementation(callback => {
                callback(0);
                return 1;
            });
        Object.defineProperty(window, "scrollX", { configurable: true, value: 12 });
        Object.defineProperty(window, "scrollY", { configurable: true, value: 540 });

        render(<PaginatedContent />);

        fireEvent.click(screen.getByRole("button", { name: "Next" }));

        expect(screen.getByText("Rows for page 2")).toBeInTheDocument();
        expect(scrollTo).toHaveBeenCalledWith(12, 540);

        scrollTo.mockRestore();
        requestAnimationFrame.mockRestore();
        Object.defineProperty(window, "scrollX", { configurable: true, value: 0 });
        Object.defineProperty(window, "scrollY", { configurable: true, value: 0 });
    });
});
