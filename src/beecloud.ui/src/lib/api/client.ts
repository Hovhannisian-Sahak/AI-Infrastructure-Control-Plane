const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

if (!API_BASE_URL) {
  throw new Error("NEXT_PUBLIC_API_BASE_URL is not configured");
}

export class ApiClient {
  private readonly baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  private async handleError(
      response: Response,
      action: string,
  ): Promise<never> {
    switch (response.status) {
      case 400:
        throw new Error(
            `The ${action} request is invalid.`,
        );

      case 401:
        throw new Error(
            `You are not authorized to ${action} this resource.`,
        );

      case 403:
        throw new Error(
            `You do not have permission to ${action} this resource.`,
        );

      case 404:
        throw new Error(
            "The resource was not found.",
        );

      case 409:
        if (response.headers.get("content-type")?.includes("json")) {
          const problem: unknown = await response.json();
          if (
              typeof problem === "object" &&
              problem !== null &&
              "detail" in problem &&
              typeof problem.detail === "string"
          ) {
            throw new Error(problem.detail);
          }
        }
        throw new Error(
            "The request conflicts with the current state of the resource.",
        );

      case 500:
        throw new Error(
            `The server could not ${action} the resource.`,
        );

      default:
        throw new Error(
            `Failed to ${action} the resource. Please try again.`,
        );
    }
  }

  async get<T>(path: string): Promise<T> {
    const response = await fetch(
        `${this.baseUrl}${path}`,
    );

    if (!response.ok) {
      await this.handleError(response, "get");
    }

    return response.json() as Promise<T>;
  }

  async post<TResponse, TRequest>(
      path: string,
      body: TRequest,
  ): Promise<TResponse> {
    const response = await fetch(
        `${this.baseUrl}${path}`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(body),
        },
    );

    if (!response.ok) {
      await this.handleError(response, "create");
    }

    if (response.status === 204) {
      return undefined as TResponse;
    }

    return response.json() as Promise<TResponse>;
  }

  async put<TResponse, TRequest>(
      path: string,
      body: TRequest,
  ): Promise<TResponse> {
    const response = await fetch(
        `${this.baseUrl}${path}`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(body),
        },
    );

    if (!response.ok) {
      await this.handleError(response, "update");
    }

    return response.json() as Promise<TResponse>;
  }

  async delete(path: string): Promise<void> {
    const response = await fetch(
        `${this.baseUrl}${path}`,
        {
          method: "DELETE",
        },
    );

    if (!response.ok) {
      await this.handleError(response, "delete");
    }
  }
}

export const apiClient = new ApiClient(API_BASE_URL);