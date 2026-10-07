import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import Pagination from "../Pagination";
import CursorPagination from "../CursorPagination";

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

function CursorPaginatedContent() {
    const [cursors, setCursors] = useState({
        next: "older",
        previous: null as string | null,
    });

    return (
        <CursorPagination
            nextCursor={cursors.next}
            previousCursor={cursors.previous}
            ariaLabel="History pages"
            onNavigate={() => {
                setCursors({ next: null, previous: "newer" });
            }}
        />
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

        const navigation = screen.getByRole("navigation", { name: "Test pages" });
        jest.spyOn(navigation, "getBoundingClientRect")
            .mockReturnValueOnce({ top: 300 } as DOMRect)
            .mockReturnValueOnce({ top: 500 } as DOMRect);
        fireEvent.pointerDown(navigation);
        Object.defineProperty(window, "scrollY", { configurable: true, value: 700 });
        fireEvent.click(screen.getByRole("button", { name: "Next" }));

        expect(screen.getByText("Rows for page 2")).toBeInTheDocument();
        expect(scrollTo).toHaveBeenCalledWith(12, 900);

        scrollTo.mockRestore();
        requestAnimationFrame.mockRestore();
        Object.defineProperty(window, "scrollX", { configurable: true, value: 0 });
        Object.defineProperty(window, "scrollY", { configurable: true, value: 0 });
    });

    it("preserves cursor pagination position when results update immediately", () => {
        const scrollTo = jest.spyOn(window, "scrollTo").mockImplementation(() => {});
        const requestAnimationFrame = jest
            .spyOn(window, "requestAnimationFrame")
            .mockImplementation(callback => {
                callback(0);
                return 1;
            });
        Object.defineProperty(window, "scrollX", { configurable: true, value: 12 });
        Object.defineProperty(window, "scrollY", { configurable: true, value: 540 });

        render(<CursorPaginatedContent />);

        const navigation = screen.getByRole("navigation", { name: "History pages" });
        jest.spyOn(navigation, "getBoundingClientRect")
            .mockReturnValueOnce({ top: 300 } as DOMRect)
            .mockReturnValueOnce({ top: 500 } as DOMRect);
        fireEvent.pointerDown(navigation);
        fireEvent.click(screen.getByRole("button", { name: "Older" }));

        expect(screen.getByRole("button", { name: "Newer" })).toBeInTheDocument();
        expect(scrollTo).toHaveBeenCalledWith(12, 740);

        scrollTo.mockRestore();
        requestAnimationFrame.mockRestore();
        Object.defineProperty(window, "scrollX", { configurable: true, value: 0 });
        Object.defineProperty(window, "scrollY", { configurable: true, value: 0 });
    });
});
