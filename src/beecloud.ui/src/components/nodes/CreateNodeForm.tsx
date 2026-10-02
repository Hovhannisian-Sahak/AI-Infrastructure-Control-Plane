"use client";

import { useState } from "react";

type CreateNodeFormProps = {
    onSubmit: (request: {
        name: string;
        gpuModel: string;
        gpuCount: number;
    }) => void;
};

export default function CreateNodeForm({
                                           onSubmit,
                                       }: CreateNodeFormProps) {
    const [name, setName] = useState("");
    const [gpuModel, setGpuModel] = useState("");
    const [gpuCount, setGpuCount] = useState("1");

    const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        onSubmit({
            name,
            gpuModel,
            gpuCount: Number(gpuCount),
        });
    };

    return (
        <form onSubmit={handleSubmit}>
            <div>
                <label htmlFor="node-name">Node Name</label>
                <input
                    id="node-name"
                    value={name}
                    onChange={(event) => setName(event.target.value)}
                />
            </div>

            <div>
                <label htmlFor="gpu-model">GPU Model</label>
                <input
                    id="gpu-model"
                    value={gpuModel}
                    onChange={(event) => setGpuModel(event.target.value)}
                />
            </div>

            <div>
                <label htmlFor="gpu-count">GPU Count</label>
                <input
                    id="gpu-count"
                    type="number"
                    min="1"
                    value={gpuCount}
                    onChange={(event) => setGpuCount(event.target.value)}
                />
            </div>

            <button type="submit">
                Create Node
            </button>
        </form>
    );
}