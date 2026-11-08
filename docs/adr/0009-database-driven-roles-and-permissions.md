# ADR 0009. Drive authorization from roles and permissions stored in the database

**Status.** Accepted
**Date.** 2026-11-08
**Deciders.** Group 6
**Related backlog items.** MET-C-022 (operational restrictions), permission-based access control for restriction actions

## Context

Endpoints were protected only by `[Authorize]`, which checks that a valid JWT exists but says nothing about what the user may do. The only role concept was a `UserRole` enum column on `Users`, so a user had exactly one hardcoded role and changing what a role could do meant a code change and a migration. The workflow also needs separation of duties: a user who can create a restriction and holds an approving role must not approve their own restriction.

## Decision

> We will model authorization as permissions granted to roles, with users assigned roles, all stored as database rows (`Permissions`, `Roles`, `RolePermissions`, `UserRoleAssignments`), and enforce it with a `[RequiresPermission("...")]` attribute that checks the authenticated user has at least one role granting the named permission. The `User.Role` enum column is removed.

`RequiresPermissionAttribute` (`Backend/Authorization/`) is an async authorization filter: it reads the user id from the JWT `sub` claim, asks `PermissionService` whether any of the user's roles grants the permission, and returns 401 when unauthenticated or 403 when the permission is missing. Because the check is a join over database tables, granting or revoking a permission from a role, or a role from a user, is a data change with no deployment. Additionally, `OperationalRestrictionService.ApproveRestrictionAsync` throws `SelfApprovalNotAllowedException` when the approver is the restriction's originator, regardless of their permissions.

## Consequences

### What becomes easier

- New actions can be protected with one attribute; new roles and permissions are inserted as rows, not code.
- A user can hold several roles (e.g. Originator + DutyOperationsManager), resolved by "at least one role grants it".
- Separation of duties on approval is enforced in the service layer, so it holds no matter which endpoint calls it.

### What becomes harder

- Every protected endpoint needs a permission that exists in the database and is granted to the right roles; forgetting the seed row means everyone gets 403, not 200.
- The permission check is one extra database query per request (acceptable at this scale; could be cached per request later).
- `Signature.SigneeRole` had to become a plain string (role name at signing time) instead of the enum, and the originator signature is now identified by `SigneeId == OriginatorUserId` rather than by role.

### What this commits us to

- Controllers must keep using DTOs and must not re-implement permission checks; the attribute plus `IPermissionService` is the single mechanism.
- Removing the `User.Role` column (migration `20261108100509_RemoveLegacyUserRoleEnum`) is expensive to reverse; role assignments for existing users are seeded from a fixed email→role map in `DbSeeder`.

## Alternatives considered

**Keep the `UserRole` enum and hardcode role checks in controllers/services.** Rejected: one role per user, and every change to what a role may do is a code change — exactly what the workflow needs to avoid.

**Claims-based permissions in the JWT.** Rejected: permissions would be frozen into the token until re-login, so revoking a permission would not take effect until the token expires, and the token would grow with every permission.

**Policy-based authorization with hardcoded policies (`services.AddAuthorization(options => options.AddPolicy(...))`).** Rejected: policies are still code, so updating a role's permissions would require a code change, which is what this ADR exists to avoid.

## Notes

Permission names are matched case-sensitively after trimming; well-known names live in `Backend/Authorization/Permissions.cs` and role names in `RoleNames.cs`. Seeded role→permission grants are in `DbSeeder.RolePermissionsByRoleName` — change them there (or directly in the DB) without touching code. Covered by `RequiresPermissionAttributeTests`, `PermissionServiceTests`, `SelfApprovalTests`, and `PermissionEndpointTests`.
