namespace Backend.Authorization;

// Well-known role names stored in the database. Roles are data, not code:
// a role with one of these names can be granted any permission by inserting
// rows into RolePermissions, and users get roles through UserRoleAssignments.
public static class RoleNames
{
    public const string Originator = "Originator";
    public const string DutyOperationsManager = "DutyOperationsManager";
    public const string Operator = "Operator";
    public const string ControlroomSupervisor = "ControlroomSupervisor";
    public const string Betriebsleiter = "Betriebsleiter";
}
