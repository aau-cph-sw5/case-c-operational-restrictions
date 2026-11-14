// Backend UserRole enum order. ASP.NET serialises enums as numbers
// unless a JsonStringEnumConverter is registered, so accept both forms.
export type UserRole =
  | "Originator"
  | "DutyOperationsManager"
  | "Operator"
  | "ControlroomSupervisor"
  | "Betriebsleiter"
  | "Unknown";

const numericRoles: UserRole[] = [
  "Originator",
  "DutyOperationsManager",
  "Operator",
  "ControlroomSupervisor",
  "Betriebsleiter",
];

export function normalizeRole(raw: unknown): UserRole {
  if (typeof raw === "number") return numericRoles[raw] ?? "Unknown";

  if (typeof raw === "string") {
    const match = (numericRoles as string[]).find(
      (role) => role.toLowerCase() === raw.toLowerCase(),
    );
    return (match as UserRole) ?? "Unknown";
  }

  return "Unknown";
}
