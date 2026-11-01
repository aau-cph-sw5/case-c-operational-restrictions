# ADR 0009. Enforce permissions with a RequirePermission attribute and a multi-role database check

**Status.** Accepted
**Date.** 2026-10-26
**Deciders.** Group 5
**Related backlog items.** MET-C-022, Roles and permission matrix

## Context

Roles and permissions are stored in the database as a user -> role -> permission connection (`User` -> `UserRoleAssignment` -> `Role` -> `RolePermissionAssignment` -> `PermissionEntity`). A user can hold multiple roles at the same time: `UserRoleAssignment` is a join table keyed on (user, role name), so nothing stops the same user from being, for example, both an Originator and an Operator.

Endpoints need to restrict access by permission (for example `OperationalRestriction:read`), not by role. The permission matrix itself is still waiting on Metro (MET-C-022), so the concrete permission names and role assignments will keep changing while the code is being written.

The backend already identifies the caller on every authenticated request: `ApiControllerBase.CurrentUserId` reads the `sub` claim from the JWT.

## Decision

We will enforce permissions with a `RequirePermissionAttribute` placed directly on controller endpoints, taking a single permission name (for example `[RequirePermission("OperationalRestriction:read")]`). The attribute resolves the current user from the `sub` claim, delegates the check to `IPermissionService`, and returns 401 when there is no valid logged-in user and 403 when the user lacks the permission.

A user has a permission when **any one** of their roles grants it. `PermissionService.UserHasPermissionAsync` evaluates this as an `AnyAsync` over the user's role assignments, so multiple roles are unioned: each role independently grants, and there is no deny or precedence between roles.

## Consequences

### What becomes easier

- Endpoints declare their access rule next to the route, in the same style as `[Authorize]`, with a permission name instead of a hard-coded role list. When Metro delivers the permission matrix, only database rows change, not endpoint code.
- Multiple roles per user need no special handling: the union semantics fall out of the join-table query directly.
- The check reads live data, so a role or permission granted or revoked after login takes effect on the next request, without waiting for the token to expire.
- The check is one SQL query per decorated endpoint and lives in `PermissionService`, where it can be unit tested without a controller.

### What this commits us to

Permission names are the API contract between the database and endpoint code, in the `Resource:action` form. New permissions must exist as `PermissionEntity` rows and be reachable through a role before any endpoint carrying them returns anything but 403.

The user -> role -> permission connection stays the single source of truth for authorization. We are not duplicating it into JWT claims, so `JwtService` must not start minting permission claims without revisiting this decision.

## Alternatives considered

**Put roles or permissions in the JWT at login.** One database read at login, no per-request query. Rejected because a user's roles are many and the permission matrix is still changing: claims would go stale until token expiry, and the token would grow with every permission of every role the user holds.

**Role-based authorization with `[Authorize(Roles = "...")]`.** Built into ASP.NET Core. Rejected because it couples endpoints to role names instead of permissions, so the pending MET-C-022 matrix would directly change endpoint code, and it would not express what a user may do when the same permission is granted through different roles.

**Policy-based authorization (`IAuthorizationPolicyProvider` + `AuthorizationHandler`).** The idiomatic ASP.NET Core mechanism for dynamic requirements, and it integrates with `[Authorize]` and authentication challenge handling. Rejected for now because it needs three cooperating classes to express what one attribute and one service method express here, and no current requirement needs policies composed from multiple requirements.

**Check the permission inside each service or controller method.** No framework plumbing at all. Rejected because the check is cross-cutting: it would be repeated in every method, and a forgotten call would silently allow access instead of silently denying it.

**Middleware reading the permission from endpoint metadata.** Keeps enforcement in one place and out of the controllers. Rejected because the middleware would have to reflect over endpoint attributes to find the requirement, which is exactly what an authorization filter is for.

## Notes

`RequirePermissionAttribute` is in `Backend/Authorization/RequirePermissionAttribute.cs`, the check in `Backend/Services/Auth/PermissionService.cs`, registered as `IPermissionService` in `Program.cs`. Until the permission matrix is seeded, every endpoint carrying the attribute returns 403; that is expected behavior.

Revisit if the permission matrix introduces deny rules, if the per-request database check becomes a measured bottleneck, or if the frontend needs a way to discover which permissions the current user has.
