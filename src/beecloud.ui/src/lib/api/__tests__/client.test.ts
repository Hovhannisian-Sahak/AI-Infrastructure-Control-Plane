import { ApiClient } from "../client";

describe("ApiClient", () => {
    const originalFetch = globalThis.fetch;

    afterEach(() => {
        globalThis.fetch = originalFetch;
    });

    it("uses the server detail for conflict responses", async () => {
        globalThis.fetch = jest.fn().mockResolvedValue({
            ok: false,
            status: 409,
            headers: {
                get: () => "application/problem+json",
            },
            json: async () => ({
                detail: "Invalid node state transition: Quarantined -> Stopping.",
            }),
        } as unknown as Response);

        const client = new ApiClient("http://localhost");

        await expect(client.post("/nodes/node-1/restart", undefined))
            .rejects.toThrow(
                "Invalid node state transition: Quarantined -> Stopping.",
            );
    });
});
