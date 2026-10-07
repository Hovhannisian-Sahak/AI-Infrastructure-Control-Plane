import {
    HEALTH_TIME_RANGES,
    getHealthRangeStart,
} from "../healthTimeRange";

describe("healthTimeRange", () => {
    it("contains the supported ranges", () => {
        expect(
            HEALTH_TIME_RANGES.map(item => item.value),
        ).toEqual([
            "1h",
            "6h",
            "24h",
            "7d",
        ]);
    });

    it("calculates a one hour range", () => {
        const now = new Date(
            "2026-10-07T13:00:00Z",
        );

        expect(
            getHealthRangeStart("1h", now).toISOString(),
        ).toBe(
            "2026-10-07T12:00:00.000Z",
        );
    });

    it("calculates a six hour range", () => {
        const now = new Date(
            "2026-10-07T13:00:00Z",
        );

        expect(
            getHealthRangeStart("6h", now).toISOString(),
        ).toBe(
            "2026-10-07T07:00:00.000Z",
        );
    });

    it("calculates a twenty four hour range", () => {
        const now = new Date(
            "2026-10-07T13:00:00Z",
        );

        expect(
            getHealthRangeStart("24h", now).toISOString(),
        ).toBe(
            "2026-10-06T13:00:00.000Z",
        );
    });

    it("calculates a seven day range", () => {
        const now = new Date(
            "2026-10-07T13:00:00Z",
        );

        expect(
            getHealthRangeStart("7d", now).toISOString(),
        ).toBe(
            "2026-09-30T13:00:00.000Z",
        );
    });
});