// API:
// Raw HTTP calls to the .NET backend's restriction endpoints.
import type {
  CreateRestrictionRequest,
  RestrictionDto,
} from "../types/restriction";

// 400 Bad Request so the UI can show every server message.
export class RestrictionValidationError extends Error {
  messages: string[];

  constructor(messages: string[]) {
    super(messages.join(" "));
    this.name = "RestrictionValidationError";
    this.messages = messages;
  }
}

// Reads ASP.NET ProblemDetails.
async function readValidationMessages(response: Response): Promise<string[]> {
  try {
    const body = await response.json();
    if (body?.errors && typeof body.errors === "object") {
      const messages = Object.values(body.errors).flat() as string[];
      if (messages.length > 0) return messages;
    }
    if (typeof body?.detail === "string") return [body.detail];
    if (typeof body?.title === "string") return [body.title];
  } catch {
    // body was not JSON, fall through
  }
  return ["The server rejected the restriction as invalid."];
}

export async function createRestriction(
  request: CreateRestrictionRequest,
  token: string,
): Promise<RestrictionDto> {
  const response = await fetch("/api/restrictions", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(request),
  });

  if (response.status === 400) {
    throw new RestrictionValidationError(await readValidationMessages(response));
  }

  if (!response.ok) {
    throw new Error(`Could not create restriction (HTTP ${response.status}).`);
  }

  return response.json();
}