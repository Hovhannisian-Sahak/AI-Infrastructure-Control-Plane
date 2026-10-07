export type PageResponse<T> = {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
};

export type CursorPageResponse<T> = {
    items: T[];
    nextCursor: string | null;
    previousCursor: string | null;
};
