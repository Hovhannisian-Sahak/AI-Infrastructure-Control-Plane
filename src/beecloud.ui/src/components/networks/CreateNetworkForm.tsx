"use client";

import {
    FormEvent,
    useState,
} from "react";
import { CreateNetworkRequest } from "@/types/network";
import styles from "./CreateNetworkForm.module.css";

type CreateNetworkFormProps = {
    onSubmit: (
        request: CreateNetworkRequest,
    ) => Promise<void>;
    isSubmitting: boolean;
};

export default function CreateNetworkForm({
                                              onSubmit,
                                              isSubmitting,
                                          }: CreateNetworkFormProps) {
    const [name, setName] = useState("");
    const [description, setDescription] =
        useState("");

    const [nameError, setNameError] =
        useState<string | null>(null);

    const [descriptionError, setDescriptionError] =
        useState<string | null>(null);

    const handleSubmit = async (
        event: FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const trimmedName = name.trim();
        const trimmedDescription =
            description.trim();

        if (!trimmedName) {
            setNameError(
                "Network name is required.",
            );
            return;
        }

        if (trimmedName.length > 100) {
            setNameError(
                "Network name must be 100 characters or fewer.",
            );
            return;
        }

        setNameError(null);

        if (trimmedDescription.length > 500) {
            setDescriptionError(
                "Network description must be 500 characters or fewer.",
            );
            return;
        }

        setDescriptionError(null);

        await onSubmit({
            name: trimmedName,
            description:
                trimmedDescription || undefined,
        });

        setName("");
        setDescription("");
    };

    const handleNameChange = (
        value: string,
    ) => {
        setName(value);

        if (nameError) {
            setNameError(null);
        }
    };

    const handleDescriptionChange = (
        value: string,
    ) => {
        setDescription(value);

        if (descriptionError) {
            setDescriptionError(null);
        }
    };

    return (
        <section className={styles.container}>
            <div className={styles.heading}>
                <div className={styles.headingIcon} aria-hidden="true">
                    <span className={styles.networkIcon}>N</span>
                </div>

                <div className={styles.headingContent}>
                    <h3 className={styles.title}>
                        Create Network
                    </h3>

                    <p className={styles.subtitle}>
                        Create a network that compute nodes
                        can attach to.
                    </p>
                </div>
            </div>

            <form
                className={styles.form}
                onSubmit={handleSubmit}
                noValidate
            >
                <div className={styles.fields}>
                    <div className={styles.field}>
                        <label
                            className={styles.label}
                            htmlFor="network-name"
                        >
                            Network Name
                            <span
                                className={styles.required}
                                aria-hidden="true"
                            />
                        </label>

                        <input
                            className={`${styles.input} ${
                                nameError
                                    ? styles.inputError
                                    : ""
                            }`}
                            id="network-name"
                            type="text"
                            value={name}
                            maxLength={100}
                            onChange={(event) =>
                                handleNameChange(
                                    event.target.value,
                                )
                            }
                            placeholder="e.g. gpu-production"
                            aria-invalid={
                                nameError ? "true" : "false"
                            }
                            aria-describedby={
                                nameError
                                    ? "network-name-error"
                                    : undefined
                            }
                        />

                        {nameError && (
                            <p
                                id="network-name-error"
                                className={styles.validationError}
                                role="alert"
                            >
                                {nameError}
                            </p>
                        )}
                    </div>

                    <div className={styles.field}>
                        <label
                            className={styles.label}
                            htmlFor="network-description"
                        >
                            Description
                            <span className={styles.optional}>
                Optional
              </span>
                        </label>

                        <textarea
                            className={`${styles.textarea} ${
                                descriptionError
                                    ? styles.inputError
                                    : ""
                            }`}
                            id="network-description"
                            value={description}
                            maxLength={500}
                            onChange={(event) =>
                                handleDescriptionChange(
                                    event.target.value,
                                )
                            }
                            placeholder="Describe the purpose of this network..."
                            rows={3}
                            aria-invalid={
                                descriptionError
                                    ? "true"
                                    : "false"
                            }
                            aria-describedby={
                                descriptionError
                                    ? "network-description-error"
                                    : undefined
                            }
                        />

                        {descriptionError && (
                            <p
                                id="network-description-error"
                                className={styles.validationError}
                                role="alert"
                            >
                                {descriptionError}
                            </p>
                        )}
                    </div>
                </div>

                <div className={styles.footer}>
                    <div className={styles.hint}>
            <span
                className={styles.hintIcon}
                aria-hidden="true"
            >
              i
            </span>

                        <span>
              Maximum{" "}
                            <strong>4 node attachments</strong>
            </span>
                    </div>

                    <button
                        className={styles.button}
                        type="submit"
                        disabled={
                            isSubmitting ||
                            name.trim().length === 0
                        }
                    >
                        {isSubmitting ? (
                            <>
                <span
                    className={styles.buttonSpinner}
                    aria-hidden="true"
                />

                                Creating network...
                            </>
                        ) : (
                            <>
                <span
                    className={styles.buttonIcon}
                    aria-hidden="true"
                >
                  +
                </span>

                                Create Network

                                <span
                                    className={styles.buttonArrow}
                                    aria-hidden="true"
                                >
                  →
                </span>
                            </>
                        )}
                    </button>
                </div>
            </form>
        </section>
    );
}