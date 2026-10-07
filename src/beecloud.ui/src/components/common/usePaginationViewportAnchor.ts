"use client";

import { useCallback, useLayoutEffect, useRef } from "react";

export function usePaginationViewportAnchor(
    pageKey: string | number,
    busy: boolean,
    waitForBusy = false,
) {
    const element = useRef<HTMLElement | null>(null);
    const anchorTop = useRef<number | null>(null);
    const sawBusy = useRef(false);

    const preservePosition = useCallback(() => {
        if (anchorTop.current !== null) {
            return;
        }

        anchorTop.current = element.current?.getBoundingClientRect().top ?? null;
        sawBusy.current = false;
    }, []);

    useLayoutEffect(() => {
        if (busy) {
            if (anchorTop.current !== null) {
                sawBusy.current = true;
            }
            return;
        }

        if (
            anchorTop.current === null ||
            (waitForBusy && !sawBusy.current)
        ) {
            return;
        }

        const frame = window.requestAnimationFrame(() => {
            const previousTop = anchorTop.current;
            const currentTop = element.current?.getBoundingClientRect().top;
            if (previousTop !== null && currentTop !== undefined) {
                window.scrollTo(
                    window.scrollX,
                    window.scrollY + currentTop - previousTop,
                );
            }
            anchorTop.current = null;
            sawBusy.current = false;
        });

        return () => window.cancelAnimationFrame(frame);
    }, [busy, pageKey, waitForBusy]);

    return {
        element,
        preservePosition,
        onPointerDownCapture: preservePosition,
    };
}
