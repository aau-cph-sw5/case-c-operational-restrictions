import { describe, expect, it } from "vitest";
import { formatUtc, parseApiDate } from "../lib/dateFormat";

describe("dateFormat", () => {
  it("formats a UTC timestamp as YYYY-MM-DD HH:mm:ss UTC", () => {
    expect(formatUtc("2026-10-22T09:05:07Z")).toBe("2026-10-22 09:05:07 UTC");
  });

  it("treats timestamps without a timezone designator as UTC", () => {
    expect(parseApiDate("2026-10-22T09:05:07").toISOString()).toBe(
      "2026-10-22T09:05:07.000Z",
    );
    expect(formatUtc("2026-10-22T09:05:07.123456")).toBe(
      "2026-10-22 09:05:07 UTC",
    );
  });

  it("converts offsets to UTC", () => {
    expect(formatUtc("2026-10-22T11:05:07+02:00")).toBe(
      "2026-10-22 09:05:07 UTC",
    );
  });
});
