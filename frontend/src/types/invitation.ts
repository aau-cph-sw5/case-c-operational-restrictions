export const USER_ROLE_OPTIONS = [
  { value: 0, label: "Originator" },
  { value: 1, label: "Duty Operations Manager" },
  { value: 2, label: "Operator" },
  { value: 3, label: "Control Room Supervisor" },
  { value: 4, label: "Betriebsleiter" },
] as const;

export type UserRoleValue = (typeof USER_ROLE_OPTIONS)[number]["value"];

export interface InviteUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  role: UserRoleValue;
}

export interface AcceptInviteRequest {
  token: string;
  password: string;
}
