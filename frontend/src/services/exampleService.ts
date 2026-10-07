// SERVICE:
// Handles HTTP network requests to the .NET backend
// Encapsulates endpoint URLs, headers, and error handling away from UI components
export async function sendExampleRequest(): Promise<void> {
  const response = await fetch("/api/example", { method: "POST" });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }
}
