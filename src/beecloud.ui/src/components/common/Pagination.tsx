"use client";

import { usePaginationViewportAnchor } from "./usePaginationViewportAnchor";
import styles from "./Pagination.module.css";

type PaginationProps = {
    page: number;
    pageSize: number;
    totalItems: number;
    ariaLabel: string;
    onPageChange: (page: number) => void;
    itemLabel?: string;
    disabled?: boolean;
    waitForLoading?: boolean;
};

export default function Pagination({
    page,
    pageSize,
    totalItems,
    ariaLabel,
    onPageChange,
    itemLabel = "items",
    disabled = false,
    waitForLoading = false,
}: PaginationProps) {
    const safePageSize = Math.max(1, pageSize);
    const pageCount = Math.ceil(totalItems / safePageSize);
    const currentPage = Math.min(Math.max(1, page), pageCount);
    const {
        element,
        preservePosition,
        onPointerDownCapture,
    } = usePaginationViewportAnchor(
        currentPage,
        disabled,
        waitForLoading,
    );

    if (pageCount <= 1) {
        return null;
    }

    const firstItem = (currentPage - 1) * safePageSize + 1;
    const lastItem = Math.min(currentPage * safePageSize, totalItems);

    const changePage = (nextPage: number) => {
        preservePosition();
        onPageChange(nextPage);
    };

    return (
        <nav
            ref={element}
            className={styles.pagination}
            aria-label={ariaLabel}
            onPointerDownCapture={onPointerDownCapture}
        >
            <span className={styles.summary} aria-live="polite">
                Showing {firstItem}–{lastItem} of {totalItems} {itemLabel}
            </span>
            <div className={styles.controls}>
                <button
                    type="button"
                    className={styles.button}
                    disabled={disabled || currentPage === 1}
                    onClick={() => changePage(currentPage - 1)}
                >
                    Previous
                </button>
                <span className={styles.pageStatus}>
                    Page {currentPage} of {pageCount}
                </span>
                <button
                    type="button"
                    className={styles.button}
                    disabled={disabled || currentPage === pageCount}
                    onClick={() => changePage(currentPage + 1)}
                >
                    Next
                </button>
            </div>
        </nav>
    );
}
