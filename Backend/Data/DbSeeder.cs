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
        await SeedUsersAsync(appDbContext);
        await SeedRestrictionsAsync(appDbContext);
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
        var restrictionIds = restrictions.Select(x => x.Id).ToList();

        var existingIds = await appDbContext.OperationalRestrictions
            .Where(x => restrictionIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        var missingRestrictions = restrictions.Where(x => !existingIds.Contains(x.Id)).ToList();
        if (missingRestrictions.Count == 0)
            return;

        appDbContext.OperationalRestrictions.AddRange(missingRestrictions);
        await appDbContext.SaveChangesAsync();
    }

    private static List<User> CreateUsers()
    {
        return
        [
            CreateUser(PlaceholderUserId, "Placeholder User", "placeholder@example.com", UserRole.Originator, UserWorkingStatus.AtWork),
            CreateUser(AnnaId, "Anna Jensen", "anna.jensen@example.test", UserRole.DutyOperationsManager, UserWorkingStatus.AtWork),
            CreateUser(LarsId, "Lars Nielsen", "lars.nielsen@example.test", UserRole.Operator, UserWorkingStatus.AtWork),
            CreateUser(SofieId, "Sofie Hansen", "sofie.hansen@example.test", UserRole.Operator, UserWorkingStatus.OffWork),
            CreateUser(MikkelId, "Mikkel Pedersen", "mikkel.pedersen@example.test", UserRole.ControlroomSupervisor, UserWorkingStatus.OnHoliday),
            CreateUser(CamillaId, "Camilla Andersen", "camilla.andersen@example.test", UserRole.Betriebsleiter, UserWorkingStatus.AtWork, inCharge: true),
            // Same users as db/seed.sql and the README.
            CreateUser(OliviaId, "Olivia Originator", "originator@example.test", UserRole.Originator, UserWorkingStatus.AtWork),
            CreateUser(DanId, "Dan Dom", "dom@example.test", UserRole.DutyOperationsManager, UserWorkingStatus.AtWork, inCharge: true),
            CreateUser(OscarId, "Oscar Operator", "operator@example.test", UserRole.Operator, UserWorkingStatus.AtWork)
        ];
    }

    private static List<OperationalRestriction> CreateRestrictions()
    {
        var today = DateTime.UtcNow.Date;

        return
        [
            CreateRestriction("5eed0000-0000-0000-0000-000000000001", PlaceholderUserId, RestrictionStatus.Draft, today.AddDays(7), today.AddDays(14)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000002", PlaceholderUserId, RestrictionStatus.Draft, today.AddDays(20), today.AddDays(21)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000003", PlaceholderUserId, RestrictionStatus.PendingApproval, today.AddDays(3), today.AddDays(10)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000004", PlaceholderUserId, RestrictionStatus.Active, today.AddDays(-2), today.AddDays(5)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000005", AnnaId, RestrictionStatus.Draft, today.AddDays(10), today.AddDays(12)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000006", AnnaId, RestrictionStatus.Approved, today.AddDays(1), today.AddDays(30)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000007", AnnaId, RestrictionStatus.Active, today.AddDays(-10), today.AddDays(2)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000008", AnnaId, RestrictionStatus.Cancelled, today.AddDays(-5), today.AddDays(5)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000009", LarsId, RestrictionStatus.Draft, today.AddDays(4), today.AddDays(6)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000010", LarsId, RestrictionStatus.PendingApproval, today.AddDays(8), today.AddDays(9)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000011", LarsId, RestrictionStatus.Archived, today.AddDays(-60), today.AddDays(-30)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000012", OliviaId, RestrictionStatus.Draft, today.AddDays(2), today.AddDays(3)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000013", OliviaId, RestrictionStatus.PendingApproval, today.AddDays(5), today.AddDays(19)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000014", DanId, RestrictionStatus.Active, today.AddDays(-1), today.AddDays(1)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000015", CamillaId, RestrictionStatus.Approved, today.AddDays(14), today.AddDays(28)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000016", CamillaId, RestrictionStatus.Archived, today.AddDays(-120), today.AddDays(-90)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000017", OscarId, RestrictionStatus.Draft, today.AddDays(30), today.AddDays(31)),
            CreateRestriction("5eed0000-0000-0000-0000-000000000018", SofieId, RestrictionStatus.Cancelled, today.AddDays(6), today.AddDays(8))
        ];
    }

    private static User CreateUser(
        Guid id,
        string name,
        string email,
        UserRole role,
        UserWorkingStatus workingStatus,
        bool inCharge = false)
    {
        return new User
        {
            Id = id,
            Name = name,
            Email = email,
            Role = role,
            WorkingStatus = workingStatus,
            InCharge = inCharge
        };
    }

    private static OperationalRestriction CreateRestriction(
        string id,
        Guid originatorId,
        RestrictionStatus status,
        DateTime startDate,
        DateTime endDate)
    {
        return new OperationalRestriction
        {
            Id = Guid.Parse(id),
            Originator = originatorId,
            CreatedBy = originatorId,
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow,
            OriginalOperationalRestrictionId = null
        };
    }
}
