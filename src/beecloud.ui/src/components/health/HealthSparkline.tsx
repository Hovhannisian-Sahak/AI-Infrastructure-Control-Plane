import styles from "./HealthSparkline.module.css";

type HealthSparklineProps = {
    values: Array<number | null>;
    min?: number;
    max?: number;
    ariaLabel: string;
};

const VIEWBOX_WIDTH = 120;
const VIEWBOX_HEIGHT = 32;
const PADDING_X = 2;
const PADDING_Y = 3;

type Point = {
    x: number;
    y: number;
};

export default function HealthSparkline({
                                            values,
                                            min,
                                            max,
                                            ariaLabel,
                                        }: HealthSparklineProps) {
    const validValues = values.filter(
        (value): value is number => value !== null,
    );

    if (validValues.length === 0) {
        return (
            <div
                className={styles.empty}
                role="img"
                aria-label={`${ariaLabel}: no data`}
            />
        );
    }

    const rangeMin = min ?? Math.min(...validValues);
    const rangeMax = max ?? Math.max(...validValues);
    const range = rangeMax - rangeMin;

    const getX = (index: number) => {
        if (values.length === 1) {
            return VIEWBOX_WIDTH / 2;
        }

        return (
            PADDING_X +
            (index / (values.length - 1)) *
            (VIEWBOX_WIDTH - PADDING_X * 2)
        );
    };

    const getY = (value: number) => {
        if (range === 0) {
            return VIEWBOX_HEIGHT / 2;
        }

        return (
            PADDING_Y +
            ((rangeMax - value) / range) *
            (VIEWBOX_HEIGHT - PADDING_Y * 2)
        );
    };

    const points: Array<Point | null> = values.map((value, index) =>
        value === null
            ? null
            : {
                x: getX(index),
                y: getY(value),
            },
    );

    const solidSegments: string[] = [];
    const gapSegments: string[] = [];

    let currentSolidSegment: string[] = [];

    points.forEach((point, index) => {
        if (point === null) {
            if (currentSolidSegment.length > 0) {
                solidSegments.push(currentSolidSegment.join(" "));
                currentSolidSegment = [];
            }

            return;
        }

        currentSolidSegment.push(
            currentSolidSegment.length === 0
                ? `M ${point.x},${point.y}`
                : `L ${point.x},${point.y}`,
        );

        const previousIndex = index - 1;

        if (
            previousIndex >= 0 &&
            points[previousIndex] === null
        ) {
            let previousKnownIndex = previousIndex - 1;

            while (
                previousKnownIndex >= 0 &&
                points[previousKnownIndex] === null
                ) {
                previousKnownIndex--;
            }

            if (
                previousKnownIndex >= 0 &&
                points[previousKnownIndex] !== null
            ) {
                const previousPoint = points[previousKnownIndex];

                gapSegments.push(
                    `M ${previousPoint.x},${previousPoint.y} L ${point.x},${point.y}`,
                );
            }
        }
    });

    if (currentSolidSegment.length > 0) {
        solidSegments.push(currentSolidSegment.join(" "));
    }

    return (
        <div className={styles.container}>
            <svg
                className={styles.svg}
                role="img"
                aria-label={ariaLabel}
                viewBox={`0 0 ${VIEWBOX_WIDTH} ${VIEWBOX_HEIGHT}`}
                preserveAspectRatio="none"
            >
                {solidSegments.map((segment, index) => (
                    <path
                        key={`solid-${index}`}
                        className={styles.line}
                        d={segment}
                    />
                ))}

                {gapSegments.map((segment, index) => (
                    <path
                        key={`gap-${index}`}
                        className={styles.gap}
                        d={segment}
                    />
                ))}
            </svg>
        </div>
    );
}