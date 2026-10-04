"use client";

import { useState } from "react";
import styles from "./CreateNodeForm.module.css";

type CreateNodeFormProps = {
    onSubmit: (request: {
        name: string;
        gpuModel: string;
        gpuCount: number;
    }) => void;
    isSubmitting: boolean;
};

export default function CreateNodeForm({
    onSubmit,
    isSubmitting,
}: CreateNodeFormProps) {
    const [name, setName] = useState("");
    const [gpuModel, setGpuModel] = useState("");
    const [gpuCount, setGpuCount] = useState("1");

    const [nameError, setNameError] = useState("");
    const [gpuModelError, setGpuModelError] = useState("");
    const [gpuCountError, setGpuCountError] = useState("");
    const isFormValid =
        name.trim().length > 0 &&
        name.trim().length <= 100 &&
        gpuModel.trim().length > 0 &&
        gpuModel.trim().length <= 100 &&
        Number.isInteger(Number(gpuCount)) &&
        Number(gpuCount) >= 1 &&
        Number(gpuCount) <= 16;
    const handleSubmit = (
        event: React.FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const trimmedName = name.trim();
        const trimmedGpuModel = gpuModel.trim();

        if (!trimmedName) {
            setNameError("Node name is required.");
            return;
        }

        if (trimmedName.length > 100) {
            setNameError(
                "Node name must be 100 characters or fewer.",
            );
            return;
        }

        setNameError("");

        if (!trimmedGpuModel) {
            setGpuModelError("GPU model is required.");
            return;
        }

        if (trimmedGpuModel.length > 100) {
            setGpuModelError(
                "GPU model must be 100 characters or fewer.",
            );
            return;
        }

        setGpuModelError("");

        const parsedGpuCount = Number(gpuCount);

        if (
            !Number.isInteger(parsedGpuCount) ||
            parsedGpuCount < 1 ||
            parsedGpuCount > 16
        ) {
            setGpuCountError(
                "GPU count must be between 1 and 16.",
            );
            return;
        }

        setGpuCountError("");

        onSubmit({
            name: trimmedName,
            gpuModel: trimmedGpuModel,
            gpuCount: parsedGpuCount,
        });
    };

    return (
        <div className={styles.container}>
            <div className={styles.heading}>
                <h2 className={styles.title}>
                    Create Compute Node
                </h2>

                <p className={styles.subtitle}>
                    Add a GPU compute node to your BeeCloud fleet.
                </p>
            </div>

            <form
                className={styles.form}
                onSubmit={handleSubmit}
                noValidate
            >
                <div className={styles.field}>
                    <label
                        className={styles.label}
                        htmlFor="node-name"
                    >
                        Node Name
                    </label>

                    <input
                        className={`${styles.input} ${
    nameError
        ? styles.inputError
        : ""
}`}
                        id="node-name"
                        type="text"
                        value={name}
                        placeholder="e.g. gpu-node-01"
                        maxLength={100}
                        onChange={(event) => {
                            setName(event.target.value);

                            if (event.target.value.trim()) {
                                setNameError("");
                            }
                        }}
                        aria-invalid={Boolean(nameError)}
                        aria-describedby={
                            nameError
                                ? "node-name-error"
                                : undefined
                        }
                    />

                    {nameError && (
                        <p
                            className={styles.error}
                            id="node-name-error"
                            role="alert"
                        >
                            {nameError}
                        </p>
                    )}
                </div>

                <div className={styles.field}>
                    <label
                        className={styles.label}
                        htmlFor="gpu-model"
                    >
                        GPU Model
                    </label>

                    <input
                        className={`${styles.input} ${
    gpuModelError
        ? styles.inputError
        : ""
}`}
                        id="gpu-model"
                        type="text"
                        value={gpuModel}
                        placeholder="e.g. NVIDIA A100"
                        maxLength={100}
                        onChange={(event) => {
                            setGpuModel(event.target.value);

                            if (event.target.value.trim()) {
                                setGpuModelError("");
                            }
                        }}
                        aria-invalid={Boolean(gpuModelError)}
                        aria-describedby={
                            gpuModelError
                                ? "gpu-model-error"
                                : undefined
                        }
                    />

                    {gpuModelError && (
                        <p
                            className={styles.error}
                            id="gpu-model-error"
                            role="alert"
                        >
                            {gpuModelError}
                        </p>
                    )}
                </div>

                <div className={styles.field}>
                    <label
                        className={styles.label}
                        htmlFor="gpu-count"
                    >
                        GPU Count
                    </label>

                    <input
                        className={`${styles.input} ${
    gpuCountError
        ? styles.inputError
        : ""
}`}
                        id="gpu-count"
                        type="number"
                        min="1"
                        max="16"
                        step="1"
                        value={gpuCount}
                        onChange={(event) => {
                            setGpuCount(event.target.value);

                            const value = Number(
                                event.target.value,
                            );

                            if (
                                Number.isInteger(value) &&
                                value >= 1 &&
                                value <= 16
                            ) {
                                setGpuCountError("");
                            }
                        }}
                        aria-invalid={Boolean(gpuCountError)}
                        aria-describedby={
                            gpuCountError
                                ? "gpu-count-error"
                                : undefined
                        }
                    />

                    {gpuCountError && (
                        <p
                            className={styles.error}
                            id="gpu-count-error"
                            role="alert"
                        >
                            {gpuCountError}
                        </p>
                    )}
                </div>

                <div className={styles.footer}>
                    <span className={styles.hint}>
                        The node will begin in Provisioning status.
                    </span>

                    <button
                        className={styles.button}
                        type="submit"
                        disabled={isSubmitting || !isFormValid}
                    >
                        {isSubmitting
                            ? "Creating node..."
                            : "Create Node"}
                    </button>
                </div>
            </form>
        </div>
    );
}