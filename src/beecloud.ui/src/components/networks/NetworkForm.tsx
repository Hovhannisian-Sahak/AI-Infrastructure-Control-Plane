"use client";

import { FormEvent, useState } from "react";
import { CreateNetworkRequest } from "@/types/network";

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

    const handleSubmit = async (
        event: FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        await onSubmit({
            name: name.trim(),
            description: description.trim() || undefined,
        });

        setName("");
        setDescription("");
    };

    return (
        <form onSubmit={handleSubmit}>
            <div>
                <label htmlFor="network-name">
                    Network name
                </label>

                <input
                    id="network-name"
                    type="text"
                    value={name}
                    onChange={(event) =>
                        setName(event.target.value)
                    }
                    required
                />
            </div>

            <div>
                <label htmlFor="network-description">
                    Description
                </label>

                <textarea
                    id="network-description"
                    value={description}
                    onChange={(event) =>
                        setDescription(
                            event.target.value,
                        )
                    }
                />
            </div>

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
        </form>
    );
}