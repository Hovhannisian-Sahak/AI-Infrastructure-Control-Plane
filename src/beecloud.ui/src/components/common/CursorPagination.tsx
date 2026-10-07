"use client";

import styles from "./Pagination.module.css";

type CursorPaginationProps = {
    nextCursor: string | null;
    previousCursor: string | null;
    loading?: boolean;
    ariaLabel: string;
    onNavigate: (cursor: string, previous: boolean) => void;
};

export default function CursorPagination({
    nextCursor,
    previousCursor,
    loading = false,
    ariaLabel,
    onNavigate,
}: CursorPaginationProps) {
    if (!nextCursor && !previousCursor) return null;

    return (
        <nav className={styles.pagination} aria-label={ariaLabel}>
            <span className={styles.summary}>
                Browse history records
            </span>
            <div className={styles.controls}>
                <button
                    type="button"
                    className={styles.button}
                    disabled={loading || !previousCursor}
                    onClick={() => previousCursor && onNavigate(previousCursor, true)}
                >
                    Newer
                </button>
                <button
                    type="button"
                    className={styles.button}
                    disabled={loading || !nextCursor}
                    onClick={() => nextCursor && onNavigate(nextCursor, false)}
                >
                    Older
                </button>
            </div>
        </nav>
    );
}
