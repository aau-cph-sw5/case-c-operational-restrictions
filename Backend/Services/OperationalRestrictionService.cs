using Backend.Data;
using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class OperationalRestrictionService : IOperationalRestrictionService
{
    private readonly AppDbContext _appDbContext;
    private readonly SignatureService _signatureService;

    public OperationalRestrictionService(AppDbContext appDbContext, SignatureService signatureService)
    {
        _appDbContext = appDbContext;
        _signatureService = signatureService;
    }

    // Temporary seed data until users are stored in the database.
    // Fixed Guids so the same users can be referenced between runs.
    private static readonly List<User> _users =
    [
        new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Anna Jensen",
            Email = "anna.jensen@example.com",
            PasswordHash = "seed-password-hash-1",

        },
        new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Lars Nielsen",
            Email = "lars.nielsen@example.com",
            PasswordHash = "seed-password-hash-2",

        },
        new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Sofie Hansen",
            Email = "sofie.hansen@example.com",
            PasswordHash = "seed-password-hash-3",

        },
        new User
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "Mikkel Pedersen",
            Email = "mikkel.pedersen@example.com",
            PasswordHash = "seed-password-hash-4",

        },
        new User
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "Camilla Andersen",
            Email = "camilla.andersen@example.com",
            PasswordHash = "seed-password-hash-5",

        }
    ];

    /// <summary>
    /// Creates the first version of a new operational restriction as a draft.
    /// The creator is also recorded as the originator.
    /// </summary>
    public async Task<OperationalRestrictionDto> CreateRestrictionAsync(CreateRestrictionRequest request)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidRestrictionPeriodException(request.StartDate, request.EndDate);

        var restriction = new OperationalRestriction
        {
            Id = Guid.NewGuid(),
            Originator = request.CreatedBy,
            CreatedBy = request.CreatedBy,
            Status = RestrictionStatus.Draft,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow,
            RestrictionDescription = request.RestrictionDescription,
            OriginalOperationalRestrictionId = null
        };

        _appDbContext.Add(restriction);
        await _appDbContext.SaveChangesAsync();

        return OperationalRestrictionDto.FromDalEntity(restriction);
    }

    /// <summary>
    /// Retrieves an operational restriction by its ID. Throws an exception if not found.
    /// </summary>
    public async Task<OperationalRestrictionDto> GetByIdAsync(Guid id)
    {
        var restriction = await _appDbContext.OperationalRestrictions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (restriction == null)
            throw new OperationalRestrictionNotFoundException(id);

        var restrictionDto = OperationalRestrictionDto.FromDalEntity(restriction);
        var originatorSignature = _signatureService.GetOriginatorSignature(id);
        if (originatorSignature != null)
            restrictionDto.OriginatorSignature = SignatureDto.FromDalEntity(originatorSignature);

        return restrictionDto;
    }

    public async Task<Signature> SignOriginatorAndAdvanceToPendingApprovalAsync(Guid id, Guid signerId)
    {
        var restriction = await _appDbContext.OperationalRestrictions
            .FirstOrDefaultAsync(x => x.Id == id);

        if (restriction == null)
            throw new OperationalRestrictionNotFoundException(id);

        if (restriction.Status != RestrictionStatus.Draft)
            throw new OperationalRestrictionNotSignableException(id, restriction.Status.ToString());

        await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
        Signature? signature = null;
        try
        {
            signature = _signatureService.SignOriginator(id, signerId);
            restriction.Status = RestrictionStatus.PendingApproval;
            await _appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return signature;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            finally
            {
                restriction.Status = RestrictionStatus.Draft;
                _appDbContext.Entry(restriction).State = EntityState.Unchanged;

                if (signature != null)
                    _signatureService.DiscardUnpersistedOriginatorSignature(signature);
            }

            throw;
        }
    }

    /// <summary>
    /// Retrieves all operational restrictions from the database.
    /// Returns an empty list if none are found.
    /// </summary>
    public async Task<List<OperationalRestrictionDto>> GetAllAsync()
    {
        var restrictions = await _appDbContext.OperationalRestrictions
            .AsNoTracking()
            .ToListAsync();

        return restrictions
            .Select(OperationalRestrictionDto.FromDalEntity)
            .ToList();
    }

    /// <summary>
    /// Makes a clone of a previous operational restriction with the fields from the request.
    /// </summary>
    private async Task<OperationalRestrictionDto> UpdateByIdAsync(UpdateByIdRequest request)
    {
        var originalRestriction = await _appDbContext.OperationalRestrictions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (originalRestriction == null)
            throw new OperationalRestrictionNotFoundException(request.Id);

        if (originalRestriction.OriginalOperationalRestrictionId == null)
            originalRestriction.OriginalOperationalRestrictionId = request.Id;

        if (request.Status != null)
            originalRestriction.Status = request.Status.Value;

        if (request.StartDate != null)
            originalRestriction.StartDate = request.StartDate.Value;

        if (request.EndDate != null)
            originalRestriction.EndDate = request.EndDate.Value;

        if (originalRestriction.EndDate <= originalRestriction.StartDate)
            throw new InvalidRestrictionPeriodException(
                originalRestriction.StartDate,
                originalRestriction.EndDate);

        originalRestriction.Id = Guid.NewGuid();
        originalRestriction.CreatedBy = request.EditedBy;

        _appDbContext.Add(originalRestriction);
        await _appDbContext.SaveChangesAsync();

        // Signatures on the previous version must not carry over to the edited version.
        _signatureService.InvalidateRequiredSignatures(request.Id);

        return OperationalRestrictionDto.FromDalEntity(originalRestriction);
    }
}
