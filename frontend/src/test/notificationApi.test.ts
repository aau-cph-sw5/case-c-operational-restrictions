import { afterEach, describe, expect, it, vi } from "vitest";
import { getNotifications } from "../services/notificationApi";
import * as tokenStorage from "../services/tokenStorage";

describe("notificationApi", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it("throws when no token is stored", async () => {
    vi.spyOn(tokenStorage, "getStoredToken").mockReturnValue(null);

    await expect(getNotifications()).rejects.toThrow("Not authenticated.");
  });

  it("calls GET /api/notifications with the stored token as bearer", async () => {
    const token = "test-token";
    vi.spyOn(tokenStorage, "getStoredToken").mockReturnValue(token);
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify([]), { status: 200 }),
    );
    vi.stubGlobal("fetch", fetchMock);

    await getNotifications();

    expect(fetchMock).toHaveBeenCalledExactlyOnceWith("/api/notifications", {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });
  });

  it("returns the parsed notifications with dates as Date objects", async () => {
    const responseBody = [
      {
        id: "id-1",
        operationalRestrictionId: null,
        type: "SystemInfo",
        title: "Info",
        message: "Message",
        requiresAction: false,
        createdAt: "2026-10-01T00:00:00Z",
        readAt: "2026-10-02T00:00:00Z",
        resolvedAt: null,
      },
    ];
    vi.spyOn(tokenStorage, "getStoredToken").mockReturnValue("test-token");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify(responseBody), { status: 200 }),
      ),
    );

    const result = await getNotifications();

    expect(result).toHaveLength(1);
    expect(result[0]).toEqual({
      id: "id-1",
      operationalRestrictionId: null,
      type: "SystemInfo",
      title: "Info",
      message: "Message",
      requiresAction: false,
      createdAt: new Date("2026-10-01T00:00:00Z"),
      readAt: new Date("2026-10-02T00:00:00Z"),
      resolvedAt: null,
    });
    expect(result[0].createdAt).toBeInstanceOf(Date);
    expect(result[0].readAt).toBeInstanceOf(Date);
  });

  it("throws on a non-ok response", async () => {
    vi.spyOn(tokenStorage, "getStoredToken").mockReturnValue("test-token");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(null, { status: 401 })),
    );

    await expect(getNotifications()).rejects.toThrow("HTTP 401");
  });
});
