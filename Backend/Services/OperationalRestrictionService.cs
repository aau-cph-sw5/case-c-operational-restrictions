using Backend.Data;
using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enum;
using Backend.Models.Requests;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class OperationalRestrictionService :IOperationalRestrictionService
{
    private readonly AppDbContext _appDbContext;
    private readonly SignatureService _signatureService;

    public OperationalRestrictionService(AppDbContext appDbContext, SignatureService signatureService)
    {
        _appDbContext = appDbContext;
        _signatureService = signatureService;
    }

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
            OriginalOperationalRestrictionId = null
        };

        _appDbContext.Add(restriction);
        await _appDbContext.SaveChangesAsync();

        return OperationalRestrictionDto.FromDalEntity(restriction);
    }

    /// <summary>
    /// Retrieves an operational restriction by its ID. Throws an exception if not found.
    /// Drafts are private to their originator, so another user's draft is treated as not found.
    /// </summary>
    public async Task<OperationalRestrictionDto> GetByIdAsync(Guid id, Guid currentUserId)
    {
        var restriction = await _appDbContext.OperationalRestrictions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (restriction == null || (restriction.Status == RestrictionStatus.Draft && restriction.Originator != currentUserId))
            throw new OperationalRestrictionNotFoundException(id);

        return OperationalRestrictionDto.FromDalEntity(restriction);
    }

    /// <summary>
    /// Retrieves all operational restrictions from the database. Returns an empty list if none are found.
    /// Drafts are only included for their originator.
    /// </summary>
    public async Task<List<OperationalRestrictionDto>> GetAllAsync(Guid currentUserId)
    {
        var restrictions = await _appDbContext.OperationalRestrictions
            .AsNoTracking()
            .Where(x => x.Status != RestrictionStatus.Draft || x.Originator == currentUserId)
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
        {
            originalRestriction.OriginalOperationalRestrictionId = request.Id;
        }

        if (request.Status != null)
        {
            originalRestriction.Status = request.Status.Value;
        }

        if (request.StartDate != null)
        {
            originalRestriction.StartDate = request.StartDate.Value;
        }

        if (request.EndDate != null)
        {
            originalRestriction.EndDate = request.EndDate.Value;
        }

        originalRestriction.Id = Guid.NewGuid();
        originalRestriction.CreatedBy = request.EditedBy;

        _appDbContext.Add(originalRestriction);
        await _appDbContext.SaveChangesAsync();

        // Signatures on the previous version must not carry over to the edited version.
        _signatureService.InvalidateRequiredSignatures(request.Id);

        return OperationalRestrictionDto.FromDalEntity(originalRestriction);
    }
}
