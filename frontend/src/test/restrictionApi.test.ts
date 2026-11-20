import { afterEach, describe, expect, it, vi } from "vitest";
import {
  getRestrictionById,
  getSigningAttemptStatus,
  requestChanges,
  signForImplementation,
  signRestriction,
} from "../services/restrictionApi";

describe("restrictionApi", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
  });

  it("maps backend reviewDescription and returned state", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          id: "restriction-1",
          originatorUserId: "user-1",
          state: 2,
          reviewDescription: "Add the affected track section.",
        }),
      } as Response),
    );

    const restriction = await getRestrictionById("restriction-1");

    expect(restriction.status).toBe("ReturnedForChanges");
    expect(restriction.reviewDescription).toBe(
      "Add the affected track section.",
    );
  });

  it("posts the trimmed comment to the request-changes endpoint", async () => {
    localStorage.setItem("accessToken", "token-123");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        id: "restriction-2",
        originatorUserId: "user-1",
        state: 2,
        reviewDescription: "Add the affected track section.",
      }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);

    const restriction = await requestChanges(
      "restriction-1",
      "  Add the affected track section.  ",
    );

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/restrictions/restriction-1/request-changes");
    expect(init.method).toBe("POST");
    expect(init.headers).toMatchObject({
      Authorization: "Bearer token-123",
      "Content-Type": "application/json",
    });
    expect(JSON.parse(init.body)).toEqual({
      comment: "Add the affected track section.",
    });
    expect(restriction.status).toBe("ReturnedForChanges");
  });

  it("rejects with the HTTP status when request-changes fails", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({ ok: false, status: 409 } as Response),
    );

    await expect(
      requestChanges("restriction-1", "Fix it"),
    ).rejects.toMatchObject({ status: 409 });
  });

  it("posts to the restriction signing endpoint with the restriction ID", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ id: "signature-1" }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);

    await signRestriction("restriction-1");

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/restrictions/restriction-1/sign");
    expect(init.method).toBe("POST");
  });

  it("posts to the sign-for-implementation endpoint and maps the returned signature", async () => {
    localStorage.setItem("accessToken", "token-123");
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        id: "signature-9",
        signeeId: "supervisor-1",
        signeeRole: "ControlroomSupervisor",
        signedAt: "2026-11-16T10:00:00Z",
      }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);

    const signature = await signForImplementation("restriction-1");

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/restrictions/restriction-1/sign-for-implementation");
    expect(init.method).toBe("POST");
    expect(init.headers).toMatchObject({ Authorization: "Bearer token-123" });
    expect(signature).toEqual({
      signeeId: "supervisor-1",
      signeeName: undefined,
      signeeRole: "ControlroomSupervisor",
      signedAt: "2026-11-16T10:00:00Z",
    });
  });

  it("rejects with the HTTP status when sign-for-implementation fails", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({ ok: false, status: 409 } as Response),
    );

    await expect(signForImplementation("restriction-1")).rejects.toMatchObject({
      status: 409,
    });
  });
  it("sends the attempt key as an Idempotency-Key header when signing", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ id: "signature-1" }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);

    await signRestriction("restriction-1", "key-1");

    const [, init] = fetchMock.mock.calls[0];
    expect(init.headers["Idempotency-Key"]).toBe("key-1");
  });

  it("sends the attempt key when signing for implementation", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ signeeId: "s", signeeRole: "r", signedAt: "t" }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);

    await signForImplementation("restriction-1", "key-2");

    const [, init] = fetchMock.mock.calls[0];
    expect(init.headers["Idempotency-Key"]).toBe("key-2");
  });

  it("maps the recorded device class and ignores unexpected values", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            signeeId: "s",
            signeeRole: "r",
            signedAt: "t",
            deviceClass: "Tablet",
          }),
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            signeeId: "s",
            signeeRole: "r",
            signedAt: "t",
            deviceClass: "Toaster",
          }),
        } as Response),
    );

    expect((await signForImplementation("r1")).deviceClass).toBe("Tablet");
    expect((await signForImplementation("r1")).deviceClass).toBeUndefined();
  });

  it("reads the status of a signing attempt", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        attemptKey: "key-1",
        status: "Signed",
        signature: {
          signeeId: "s",
          signeeRole: "Originator",
          signedAt: "2026-11-18T10:00:00Z",
          deviceClass: "Phone",
        },
      }),
    } as Response);
    vi.stubGlobal("fetch", fetchMock);

    const status = await getSigningAttemptStatus("key-1");

    expect(fetchMock.mock.calls[0][0]).toBe("/api/signature/attempts/key-1");
    expect(status.status).toBe("Signed");
    expect(status.signature?.deviceClass).toBe("Phone");
  });

  it("reports NotSigned when the attempt left no signature", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ attemptKey: "key-1", status: "NotSigned" }),
      } as Response),
    );

    expect(await getSigningAttemptStatus("key-1")).toEqual({
      status: "NotSigned",
      signature: undefined,
    });
  });

  it("rejects with the HTTP status when the status check fails", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({ ok: false, status: 401 } as Response),
    );

    await expect(getSigningAttemptStatus("key-1")).rejects.toMatchObject({
      status: 401,
    });
  });
});
