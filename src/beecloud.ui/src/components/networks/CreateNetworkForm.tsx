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
    const [validationError, setValidationError] =
        useState<string | null>(null);

    const handleSubmit = async (
        event: FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const trimmedName = name.trim();
        const trimmedDescription =
            description.trim();

        if (!trimmedName) {
            setValidationError(
                "Network name is required.",
            );
            return;
        }

        setValidationError(null);

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

        if (validationError) {
            setValidationError(null);
        }
    };

    return (
        <div className={styles.container}>
            <div className={styles.heading}>
                <div>
                    <h3>Create Network</h3>

                    <p>
                        Create a network that compute
                        nodes can attach to.
                    </p>
                </div>
            </div>

            <form
                className={styles.form}
                onSubmit={handleSubmit}
                noValidate
            >
                <div className={styles.field}>
                    <label htmlFor="network-name">
                        Network name
                    </label>

                    <input
                        id="network-name"
                        type="text"
                        value={name}
                        onChange={(event) =>
                            handleNameChange(
                                event.target.value,
                            )
                        }
                        placeholder="e.g. gpu-production"
                        aria-invalid={
                            validationError
                                ? "true"
                                : "false"
                        }
                        aria-describedby={
                            validationError
                                ? "network-name-error"
                                : undefined
                        }
                    />

                    {validationError && (
                        <p
                            id="network-name-error"
                            className={
                                styles.validationError
                            }
                        >
                            {validationError}
                        </p>
                    )}
                </div>

                <div className={styles.field}>
                    <label htmlFor="network-description">
                        Description
                        <span>Optional</span>
                    </label>

                    <textarea
                        id="network-description"
                        value={description}
                        onChange={(event) =>
                            setDescription(
                                event.target.value,
                            )
                        }
                        placeholder="Describe the purpose of this network..."
                        rows={3}
                    />
                </div>

                <div className={styles.footer}>
                    <span>
                        Maximum 4 node attachments
                    </span>

                    <button
                        type="submit"
                        disabled={
                            isSubmitting ||
                            name.trim().length === 0
                        }
                    >
                        {isSubmitting
                            ? "Creating..."
                            : "Create Network"}
                    </button>
                </div>
            </form>
        </div>
    );
}