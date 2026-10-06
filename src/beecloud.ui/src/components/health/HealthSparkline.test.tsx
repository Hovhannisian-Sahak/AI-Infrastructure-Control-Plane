import { render, screen } from "@testing-library/react";
import HealthSparkline from "./HealthSparkline";
import styles from "./HealthSparkline.module.css";
describe("HealthSparkline", () => {
    it("renders the sparkline with an accessible label", () => {
        render(
            <HealthSparkline
                values={[60, 65, 70, 68]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        expect(
            screen.getByRole("img", {
                name: "CPU usage trend",
            }),
        ).toBeInTheDocument();
    });

    it("renders no-data state when all values are null", () => {
        render(
            <HealthSparkline
                values={[null, null, null]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        expect(
            screen.getByRole("img", {
                name: "CPU usage trend: no data",
            }),
        ).toBeInTheDocument();
    });

    it("renders a single valid reading", () => {
        render(
            <HealthSparkline
                values={[null, 65, null]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        expect(
            screen.getByRole("img", {
                name: "CPU usage trend",
            }),
        ).toBeInTheDocument();
    });

    it("renders a solid line for continuous readings", () => {
        const { container } = render(
            <HealthSparkline
                values={[60, 65, 70, 68]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        const solidLines = container.querySelectorAll(
            'path[class*="line"]',
        );

        expect(solidLines.length).toBeGreaterThan(0);
    });

    it("renders a dashed muted segment when readings are missing", () => {
        const { container } = render(
            <HealthSparkline
                values={[60, 65, null, null, 72, 68]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        const dashedLines = container.querySelectorAll(
            'path[class*="gap"]',
        );

        expect(dashedLines.length).toBe(1);

        expect(dashedLines[0]).toHaveAttribute(
            "d",
            expect.stringContaining("M"),
        );

        expect(dashedLines[0]).toHaveAttribute(
            "d",
            expect.stringContaining("L"),
        );
    });

    it("does not render a dashed segment when there is no gap", () => {
        const { container } = render(
            <HealthSparkline
                values={[60, 65, 70, 68]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        const dashedLines = container.querySelectorAll(
            'path[class*="gap"]',
        );

        expect(dashedLines).toHaveLength(0);
    });

    it("supports multiple gaps", () => {
        const { container } = render(
            <HealthSparkline
                values={[
                    60,
                    null,
                    65,
                    70,
                    null,
                    null,
                    72,
                    68,
                ]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        const dashedLines = container.querySelectorAll(
            'path[class*="gap"]',
        );

        expect(dashedLines).toHaveLength(2);
    });

    it("renders zero values as valid measurements", () => {
        const { container } = render(
            <HealthSparkline
                values={[0, 10, 0, 20]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        expect(
            container.querySelectorAll('path[class*="line"]').length,
        ).toBeGreaterThan(0);

        expect(
            container.querySelectorAll('path[class*="gap"]'),
        ).toHaveLength(0);
    });

    it("handles identical values without producing invalid coordinates", () => {
        const { container } = render(
            <HealthSparkline
                values={[70, 70, 70, 70]}
                min={0}
                max={100}
                ariaLabel="GPU temperature trend"
            />,
        );

        const paths = container.querySelectorAll("path");

        expect(paths.length).toBeGreaterThan(0);

        paths.forEach((path) => {
            expect(path.getAttribute("d")).not.toContain("NaN");
        });
    });
    it("renders a muted class for missing-data segments", () => {
        const { container } = render(
            <HealthSparkline
                values={[60, 65, null, 72, 68]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        const dashedLine = container.querySelector(
            `path.${styles.gap}`,
        );

        expect(dashedLine).toBeInTheDocument();
        expect(dashedLine).toHaveClass(styles.gap);
    });

    it("renders the missing-data segment with the gap class", () => {
        const { container } = render(
            <HealthSparkline
                values={[60, 65, null, 72, 68]}
                min={0}
                max={100}
                ariaLabel="CPU usage trend"
            />,
        );

        const dashedLine = container.querySelector(
            `path.${styles.gap}`,
        );

        expect(dashedLine).toBeInTheDocument();
        expect(dashedLine).toHaveClass(styles.gap);
    });
});