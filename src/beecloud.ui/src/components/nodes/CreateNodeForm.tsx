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

    const [nameError, setNameError] =
        useState("");
    const [gpuModelError, setGpuModelError] =
        useState("");
    const [gpuCountError, setGpuCountError] =
        useState("");

    const handleSubmit = (
        event: React.FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const trimmedName = name.trim();
        const trimmedGpuModel = gpuModel.trim();

        if (!trimmedName) {
            setNameError(
                "Node name is required.",
            );
            return;
        }

        setNameError("");

        if (!trimmedGpuModel) {
            setGpuModelError(
                "GPU model is required.",
            );
            return;
        }

        setGpuModelError("");

        const parsedGpuCount = Number(gpuCount);

        if (
            !Number.isInteger(parsedGpuCount) ||
            parsedGpuCount <= 0
        ) {
            setGpuCountError(
                "GPU count must be greater than zero.",
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
                    Add a GPU compute node to your
                    BeeCloud fleet.
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
                        value={name}
                        placeholder="e.g. gpu-node-01"
                        onChange={(event) => {
                            setName(event.target.value);

                            if (
                                event.target.value.trim()
                            ) {
                                setNameError("");
                            }
                        }}
                        aria-invalid={Boolean(
                            nameError,
                        )}
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
                        value={gpuModel}
                        placeholder="e.g. NVIDIA A100"
                        onChange={(event) => {
                            setGpuModel(
                                event.target.value,
                            );

                            if (
                                event.target.value.trim()
                            ) {
                                setGpuModelError("");
                            }
                        }}
                        aria-invalid={Boolean(
                            gpuModelError,
                        )}
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
                        step="1"
                        value={gpuCount}
                        onChange={(event) => {
                            setGpuCount(
                                event.target.value,
                            );

                            if (
                                Number(event.target.value) >
                                0
                            ) {
                                setGpuCountError("");
                            }
                        }}
                        aria-invalid={Boolean(
                            gpuCountError,
                        )}
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
            The node will begin in Provisioning
            status.
          </span>

                    <button
                        className={styles.button}
                        type="submit"
                        disabled={isSubmitting}
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