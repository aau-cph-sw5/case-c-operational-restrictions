import { afterEach, describe, expect, it, vi } from "vitest";
import { toNotificationViewModel } from "../services/notificationMapper";
import type { NotificationDto } from "../types/notificationDto";

function makeDto(
  overrides: Partial<NotificationDto>,
): NotificationDto {
  return {
    id: "id-1",
    operationalRestrictionId: null,
    type: "SystemInfo",
    title: "Info",
    message: "Message",
    requiresAction: false,
    createdAt: "2026-10-25T12:00:00Z",
    readAt: null,
    resolvedAt: null,
    ...overrides,
  };
}

describe("notificationMapper", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("maps an unread SignatureRequired notification with action", () => {
    vi.setSystemTime(new Date("2026-10-25T12:30:00Z"));

    const result = toNotificationViewModel(
      makeDto({
        type: "SignatureRequired",
        requiresAction: true,
      }),
    );

    expect(result).toEqual({
      id: "id-1",
      category: "DRIFTSRESTRIKTION",
      title: "Info",
      message: "Message",
      time: "30 min. siden",
      status: "sent",
      unread: true,
      required: true,
      action: "Underskriv nu",
    });
  });

  it("maps a read SystemInfo notification without action", () => {
    vi.setSystemTime(new Date("2026-10-25T12:00:00Z"));

    const result = toNotificationViewModel(
      makeDto({ readAt: "2026-10-25T12:10:00Z" }),
    );

    expect(result.unread).toBe(false);
    expect(result.required).toBe(false);
    expect(result.category).toBe("SYSTEM INFO");
    expect(result.action).toBeUndefined();
  });

  it("is no longer required once resolved", () => {
    const result = toNotificationViewModel(
      makeDto({
        type: "ChangesRequested",
        requiresAction: true,
        resolvedAt: "2026-10-25T12:05:00Z",
      }),
    );

    expect(result.required).toBe(false);
    expect(result.action).toBeUndefined();
  });

  it("falls back to a title derived from the type", () => {
    const result = toNotificationViewModel(
      makeDto({ type: "SignatureRequired", title: null }),
    );

    expect(result.title).toBe("Signatur påkrævet");
  });

  it("formats older timestamps as a Danish date", () => {
    vi.setSystemTime(new Date("2026-10-25T12:00:00Z"));

    const result = toNotificationViewModel(
      makeDto({ createdAt: "2026-09-01T09:00:00Z" }),
    );

    expect(result.time).toBe("1. sep.");
  });
});
