export type HealthTimeRange =
    | "1h"
    | "6h"
    | "24h"
    | "7d";

export type HealthTimeRangeOption = {
    value: HealthTimeRange;
    label: string;
    hours: number;
};

export const HEALTH_TIME_RANGES: HealthTimeRangeOption[] = [
    {
        value: "1h",
        label: "1 hour",
        hours: 1,
    },
    {
        value: "6h",
        label: "6 hours",
        hours: 6,
    },
    {
        value: "24h",
        label: "24 hours",
        hours: 24,
    },
    {
        value: "7d",
        label: "7 days",
        hours: 24 * 7,
    },
];

export function getHealthRangeStart(
    range: HealthTimeRange,
    now = new Date(),
): Date {
    const option = HEALTH_TIME_RANGES.find(
        item => item.value === range,
    );

    if (!option) {
        return new Date(now);
    }

    return new Date(
        now.getTime() -
        option.hours * 60 * 60 * 1000,
    );
}