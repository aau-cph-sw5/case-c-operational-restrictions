import { afterEach, describe, expect, it, vi } from "vitest";
import { getRestrictionById } from "../services/restrictionApi";

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
});