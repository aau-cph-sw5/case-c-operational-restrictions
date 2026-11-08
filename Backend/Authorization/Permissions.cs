namespace Backend.Authorization;

// Well-known permission names used with [RequiresPermission(...)].
// The names are also stored in the database, so which roles grant them can
// be changed without touching code.
public static class Permissions
{
    public const string OperationalRestrictionRead = "OperationalRestriction:Read";
    public const string OperationalRestrictionCreate = "OperationalRestriction:Create";
    public const string OperationalRestrictionSign = "OperationalRestriction:Sign";
    public const string OperationalRestrictionApprove = "OperationalRestriction:Approve";
}
