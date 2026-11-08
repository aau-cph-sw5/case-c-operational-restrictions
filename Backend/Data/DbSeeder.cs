using Backend.Authorization;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Services.Auth;

using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

// Seeds the local development database with users and operational restrictions on startup.
// Every row has a fixed Id and is only inserted if it is missing, so running it again does not create duplicates.
public static class DbSeeder
{
    // All seeded users share this password, so any of them can be used when testing locally.
    public const string SeedPassword = "Password123!";

    // The placeholder user from AuthService, so the logged-in user owns some of the seeded drafts.
    public static readonly Guid PlaceholderUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid AnnaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid LarsId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SofieId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid MikkelId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid CamillaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid OliviaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid DanId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid OscarId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public static async Task SeedAsync(AppDbContext appDbContext)
    {
        if (appDbContext.Database.IsRelational() && appDbContext.Database.CurrentTransaction == null)
        {
            await using var transaction = await appDbContext.Database.BeginTransactionAsync();
            await SeedUsersAsync(appDbContext);
            await SeedRestrictionsAsync(appDbContext);
            await SeedRolesAndPermissionsAsync(appDbContext);
            await transaction.CommitAsync();
        }
        else
        {
            await SeedUsersAsync(appDbContext);
            await SeedRestrictionsAsync(appDbContext);
            await SeedRolesAndPermissionsAsync(appDbContext);
        }
    }

    // Every permission that exists in the system.
    private static readonly (string Name, string Description)[] SeedPermissions =
    [
        (Permissions.OperationalRestrictionRead, "Read operational restrictions"),
        (Permissions.OperationalRestrictionCreate, "Create operational restrictions"),
        (Permissions.OperationalRestrictionSign, "Sign operational restrictions"),
        (Permissions.OperationalRestrictionApprove, "Approve operational restrictions")
    ];

    // Which permissions each role grants. Stored in the database, so this
    // mapping can be changed with data alone.
    private static readonly Dictionary<string, string[]> RolePermissionsByRoleName = new()
    {
        [RoleNames.Originator] =
        [
            Permissions.OperationalRestrictionRead,
            Permissions.OperationalRestrictionCreate,
            Permissions.OperationalRestrictionSign
        ],
        [RoleNames.DutyOperationsManager] =
        [
            Permissions.OperationalRestrictionRead,
            Permissions.OperationalRestrictionCreate,
            Permissions.OperationalRestrictionSign,
            Permissions.OperationalRestrictionApprove
        ],
        [RoleNames.Operator] =
        [
            Permissions.OperationalRestrictionRead,
            Permissions.OperationalRestrictionCreate,
            Permissions.OperationalRestrictionSign
        ],
        [RoleNames.ControlroomSupervisor] =
        [
            Permissions.OperationalRestrictionRead,
            Permissions.OperationalRestrictionSign,
            Permissions.OperationalRestrictionApprove
        ],
        [RoleNames.Betriebsleiter] =
        [
            Permissions.OperationalRestrictionRead,
            Permissions.OperationalRestrictionCreate,
            Permissions.OperationalRestrictionSign,
            Permissions.OperationalRestrictionApprove
        ]
    };

    // The role each seeded user starts with, by email. Covers the users from
    // db/seed.sql as well, so they get role assignments too.
    private static readonly Dictionary<string, string> SeedUserRolesByEmail = new()
    {
        ["placeholder@example.com"] = RoleNames.Originator,
        ["anna.jensen@example.test"] = RoleNames.DutyOperationsManager,
        ["lars.nielsen@example.test"] = RoleNames.Operator,
        ["sofie.hansen@example.test"] = RoleNames.Operator,
        ["mikkel.pedersen@example.test"] = RoleNames.ControlroomSupervisor,
        ["camilla.andersen@example.test"] = RoleNames.Betriebsleiter,
        ["originator@example.test"] = RoleNames.Originator,
        ["dom@example.test"] = RoleNames.DutyOperationsManager,
        ["operator@example.test"] = RoleNames.Operator
    };

    // Seeds permissions, roles, the role-permission grants and the user-role
    // assignments. Everything is matched by name, so rerunning does not create
    // duplicates, and every seeded user gets a role through UserRoleAssignments.
    private static async Task SeedRolesAndPermissionsAsync(AppDbContext appDbContext)
    {
        var permissionNames = SeedPermissions.Select(p => p.Name).ToList();
        var existingPermissionNames = await appDbContext.Permissions
            .Where(p => permissionNames.Contains(p.Name))
            .Select(p => p.Name)
            .ToListAsync();
        var missingPermissions = SeedPermissions
            .Where(p => !existingPermissionNames.Contains(p.Name))
            .Select(p => new Permission { Id = Guid.NewGuid(), Name = p.Name, Description = p.Description })
            .ToList();
        if (missingPermissions.Count > 0)
        {
            appDbContext.Permissions.AddRange(missingPermissions);
            await appDbContext.SaveChangesAsync();
        }

        var roleNames = RolePermissionsByRoleName.Keys.ToList();
        var existingRoleNames = await appDbContext.Roles
            .Where(r => roleNames.Contains(r.Name))
            .Select(r => r.Name)
            .ToListAsync();
        var missingRoles = roleNames
            .Where(name => !existingRoleNames.Contains(name))
            .Select(name => new Role { Id = Guid.NewGuid(), Name = name })
            .ToList();
        if (missingRoles.Count > 0)
        {
            appDbContext.Roles.AddRange(missingRoles);
            await appDbContext.SaveChangesAsync();
        }

        var roles = await appDbContext.Roles.ToListAsync();
        var permissions = await appDbContext.Permissions.ToListAsync();

        var existingGrants = await appDbContext.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();
        var missingGrants = new List<RolePermission>();
        foreach (var role in roles)
        {
            if (!RolePermissionsByRoleName.TryGetValue(role.Name, out var grantedNames))
                continue;

            foreach (var permissionName in grantedNames)
            {
                var permission = permissions.FirstOrDefault(p => p.Name == permissionName);
                if (permission == null)
                    continue;

                if (!existingGrants.Any(g => g.RoleId == role.Id && g.PermissionId == permission.Id))
                    missingGrants.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
        }
        if (missingGrants.Count > 0)
        {
            appDbContext.RolePermissions.AddRange(missingGrants);
            await appDbContext.SaveChangesAsync();
        }

        // Every seeded user is assigned their role, including users inserted
        // by db/seed.sql (matched by email).
        var users = await appDbContext.Users
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();
        var existingAssignments = await appDbContext.UserRoleAssignments
            .Select(a => new { a.UserId, a.RoleId })
            .ToListAsync();
        var missingAssignments = new List<UserRoleAssignment>();
        foreach (var user in users)
        {
            if (!SeedUserRolesByEmail.TryGetValue(user.Email, out var roleName))
                continue;

            var role = roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null)
                continue;

            if (!existingAssignments.Any(a => a.UserId == user.Id && a.RoleId == role.Id))
                missingAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = role.Id });
        }
        if (missingAssignments.Count > 0)
        {
            appDbContext.UserRoleAssignments.AddRange(missingAssignments);
            await appDbContext.SaveChangesAsync();
        }
    }

    private static async Task SeedUsersAsync(AppDbContext appDbContext)
    {
        var users = CreateUsers();
        var userIds = users.Select(x => x.Id).ToList();
        var userEmails = users.Select(x => x.Email).ToList();

        // Also matched on email, because db/seed.sql (pnpm db:seed) inserts some of the same users with random Ids,
        // and inserting them again would break the unique email index.
        var existingUsers = await appDbContext.Users
            .Where(x => userIds.Contains(x.Id) || userEmails.Contains(x.Email))
            .Select(x => new { x.Id, x.Email })
            .ToListAsync();

        var missingUsers = users
            .Where(x => !existingUsers.Any(existing => existing.Id == x.Id || existing.Email == x.Email))
            .ToList();
        if (missingUsers.Count == 0)
            return;

        // Hashing is slow on purpose, so the shared password is only hashed once and only when needed.
        var passwordHash = new PasswordHasher().Hash(SeedPassword);
        missingUsers.ForEach(x => x.PasswordHash = passwordHash);

        appDbContext.Users.AddRange(missingUsers);
        await appDbContext.SaveChangesAsync();
    }

    private static async Task SeedRestrictionsAsync(AppDbContext appDbContext)
    {
        var restrictions = CreateRestrictions();
        var restrictionIds = restrictions.Select(x => x.RestrictionId).ToList();

        var existingIds = await appDbContext.OperationalRestrictions
            .Where(x => restrictionIds.Contains(x.RestrictionId))
            .Select(x => x.RestrictionId)
            .ToListAsync();

        var missingRestrictions = restrictions.Where(x => !existingIds.Contains(x.RestrictionId)).ToList();
        if (missingRestrictions.Count == 0)
            return;

        appDbContext.OperationalRestrictions.AddRange(missingRestrictions);
        await appDbContext.SaveChangesAsync();
    }

    private static List<User> CreateUsers()
    {
        return
        [
            CreateUser(PlaceholderUserId, "Placeholder User", "placeholder@example.com", UserWorkingStatus.AtWork),
            CreateUser(AnnaId, "Anna Jensen", "anna.jensen@example.test", UserWorkingStatus.AtWork),
            CreateUser(LarsId, "Lars Nielsen", "lars.nielsen@example.test", UserWorkingStatus.AtWork),
            CreateUser(SofieId, "Sofie Hansen", "sofie.hansen@example.test", UserWorkingStatus.OffWork),
            CreateUser(MikkelId, "Mikkel Pedersen", "mikkel.pedersen@example.test", UserWorkingStatus.OnHoliday),
            CreateUser(CamillaId, "Camilla Andersen", "camilla.andersen@example.test", UserWorkingStatus.AtWork, inCharge: true),
            // Same users as db/seed.sql and the README.
            CreateUser(OliviaId, "Olivia Originator", "originator@example.test", UserWorkingStatus.AtWork),
            CreateUser(DanId, "Dan Dom", "dom@example.test", UserWorkingStatus.AtWork, inCharge: true),
            CreateUser(OscarId, "Oscar Operator", "operator@example.test", UserWorkingStatus.AtWork)
        ];
    }

    private static List<OperationalRestriction> CreateRestrictions()
    {
        var today = DateTime.UtcNow.Date;

        return
        [
            CreateRestriction("5eed0000-0000-0000-0000-000000000001", PlaceholderUserId, RestrictionState.Draft, today.AddDays(7), today.AddDays(14), "Sporarbejde ved Nørreport", "REF-2026-001"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000002", PlaceholderUserId, RestrictionState.Draft, today.AddDays(20), today.AddDays(21), "Hastighedsbegrænsning M1", "REF-2026-002"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000003", PlaceholderUserId, RestrictionState.PendingDutyOperationsManagerApproval, today.AddDays(3), today.AddDays(10), "Afspærring af perron 2", "REF-2026-003"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000004", PlaceholderUserId, RestrictionState.Active, today.AddDays(-2), today.AddDays(5), "Sporskiftefejl ved Kongens Nytorv", "REF-2026-004"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000005", AnnaId, RestrictionState.Draft, today.AddDays(10), today.AddDays(12), "Signalinspektion vest", "REF-2026-005"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000006", AnnaId, RestrictionState.Approved, today.AddDays(1), today.AddDays(30), "Vedligeholdelse af kørestrøm", "REF-2026-006"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000007", AnnaId, RestrictionState.Active, today.AddDays(-10), today.AddDays(2), "Ventilationsarbejde i tunnel", "REF-2026-007"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000008", AnnaId, RestrictionState.CancelledRefused, today.AddDays(-5), today.AddDays(5), "Aflyst perronlukning", "REF-2026-008"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000009", LarsId, RestrictionState.Draft, today.AddDays(4), today.AddDays(6), "Belægningsarbejde forplads", "REF-2026-009"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000010", LarsId, RestrictionState.PendingDutyOperationsManagerApproval, today.AddDays(8), today.AddDays(9), "Testkørsel af nyt materiel", "REF-2026-010"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000011", LarsId, RestrictionState.Archived, today.AddDays(-60), today.AddDays(-30), "Vinterforberedelse 2025", "REF-2025-099"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000012", OliviaId, RestrictionState.Draft, today.AddDays(2), today.AddDays(3), "Rensning af drænkanaler", null),
            CreateRestriction("5eed0000-0000-0000-0000-000000000013", OliviaId, RestrictionState.PendingDutyOperationsManagerApproval, today.AddDays(5), today.AddDays(19), "Belysningsudskiftning perron", "REF-2026-013"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000014", DanId, RestrictionState.Active, today.AddDays(-1), today.AddDays(1), "Akut sporkontrol", "REF-2026-014"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000015", CamillaId, RestrictionState.Approved, today.AddDays(14), today.AddDays(28), "Ombygning af nødsystemer", "REF-2026-015"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000016", CamillaId, RestrictionState.Archived, today.AddDays(-120), today.AddDays(-90), "Sommerkøreplan tilpasning", "REF-2025-050"),
            CreateRestriction("5eed0000-0000-0000-0000-000000000017", OscarId, RestrictionState.Draft, today.AddDays(30), today.AddDays(31), "Inspektion af nødudgange", null),
            CreateRestriction("5eed0000-0000-0000-0000-000000000018", SofieId, RestrictionState.CancelledRefused, today.AddDays(6), today.AddDays(8), "Aflyst sporjustering M2", "REF-2026-018")
        ];
    }

    private static User CreateUser(
        Guid id,
        string name,
        string email,
        UserWorkingStatus workingStatus,
        bool inCharge = false)
    {
        return new User
        {
            Id = id,
            Name = name,
            Email = email,
            WorkingStatus = workingStatus,
            InCharge = inCharge
        };
    }

    private static OperationalRestriction CreateRestriction(
        string id,
        Guid originatorId,
        RestrictionState state,
        DateTime startDate,
        DateTime endDate,
        string title,
        string? reference = null)
    {
        return new OperationalRestriction
        {
            RestrictionId = Guid.Parse(id),
            OriginatorUserId = originatorId,
            CreatedBy = originatorId,
            State = state,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow,
            OriginalOperationalRestrictionId = null,
            Title = title,
            Reference = reference,
            SituationDescription = $"Situation for {title}",
            RestrictionDescription = $"Driftsmæssig restriktion for {title}",
            CancellationDescription = $"Betingelser for ophævelse af {title}"
        };
    }
}
