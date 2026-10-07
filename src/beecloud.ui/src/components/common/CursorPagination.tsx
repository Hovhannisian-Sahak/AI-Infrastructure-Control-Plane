"use client";

import { usePaginationViewportAnchor } from "./usePaginationViewportAnchor";
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
    const {
        element,
        preservePosition,
        onPointerDownCapture,
    } = usePaginationViewportAnchor(
        `${nextCursor ?? ""}:${previousCursor ?? ""}`,
        loading,
    );

    if (!nextCursor && !previousCursor) return null;

    const navigate = (cursor: string, previous: boolean) => {
        preservePosition();
        onNavigate(cursor, previous);
    };

    return (
        <nav
            ref={element}
            className={styles.pagination}
            aria-label={ariaLabel}
            onPointerDownCapture={onPointerDownCapture}
        >
            <span className={styles.summary}>
                Browse history records
            </span>
            <div className={styles.controls}>
                <button
                    type="button"
                    className={styles.button}
                    disabled={loading || !previousCursor}
                    onClick={() => previousCursor && navigate(previousCursor, true)}
                >
                    Newer
                </button>
                <button
                    type="button"
                    className={styles.button}
                    disabled={loading || !nextCursor}
                    onClick={() => nextCursor && navigate(nextCursor, false)}
                >
                    Older
                </button>
            </div>
        </nav>
    );
}
