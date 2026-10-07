"use client";

import styles from "./Pagination.module.css";

type PaginationProps = {
    page: number;
    pageSize: number;
    totalItems: number;
    ariaLabel: string;
    onPageChange: (page: number) => void;
};

export default function Pagination({
    page,
    pageSize,
    totalItems,
    ariaLabel,
    onPageChange,
}: PaginationProps) {
    const safePageSize = Math.max(1, pageSize);
    const pageCount = Math.ceil(totalItems / safePageSize);

    if (pageCount <= 1) {
        return null;
    }

    const currentPage = Math.min(Math.max(1, page), pageCount);
    const firstItem = (currentPage - 1) * safePageSize + 1;
    const lastItem = Math.min(currentPage * safePageSize, totalItems);

    return (
        <nav className={styles.pagination} aria-label={ariaLabel}>
            <span className={styles.summary} aria-live="polite">
                Showing {firstItem}–{lastItem} of {totalItems}
            </span>
            <div className={styles.controls}>
                <button
                    type="button"
                    className={styles.button}
                    disabled={currentPage === 1}
                    onClick={() => onPageChange(currentPage - 1)}
                >
                    Previous
                </button>
                <span className={styles.pageStatus}>
                    Page {currentPage} of {pageCount}
                </span>
                <button
                    type="button"
                    className={styles.button}
                    disabled={currentPage === pageCount}
                    onClick={() => onPageChange(currentPage + 1)}
                >
                    Next
                </button>
            </div>
        </nav>
    );
}
