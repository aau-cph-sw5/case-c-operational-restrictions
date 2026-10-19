// Gr4 current version>
using Backend.Data;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;

namespace Backend.Services;

//TODO: This interface should be seperated from the class
public interface IOperationalRestrictionService
{
    Task<OperationalRestriction> CreateRestrictionAsync(
        OperationalRestrictionDto data);

    Task<OperationalRestriction> EditRestrictionAsync(
        Guid id,
        OperationalRestrictionDto updateRestrictionDto);

    Task<OperationalRestriction> ApproveRestrictionAsync(
        Guid id,
        Guid approverId);

    Task<OperationalRestriction> DenyRestrictionAsync(
        Guid id,
        Guid approverId,
        string reason);

    Task<OperationalRestriction> ArchiveRestrictionAsync(Guid id);

    Task<OperationalRestriction> AssignOperatorAsync(
        Guid id,
        Guid operatorId);

    Task<OperationalRestriction?> GetRestrictionByIdAsync(Guid id);

    Task<List<OperationalRestriction>> GetActiveRestrictionsAsync();
}

public class OperationalRestrictionService
    : IOperationalRestrictionService
{
    private readonly RestrictionLifecycleService _lifecycleService;
    private readonly IOperationalRestrictionRepository _repository;

    public OperationalRestrictionService(
        RestrictionLifecycleService lifecycleService,
        IOperationalRestrictionRepository repository)
    {
        _lifecycleService = lifecycleService;
        _repository = repository;
    }

    public async Task<OperationalRestriction> CreateRestrictionAsync(
        OperationalRestrictionDto data)
    {
        _lifecycleService.EnsureCanCreate(data);

        var now = DateTime.UtcNow;

        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = data.OriginatorUserId,
            RestrictionDescription = data.RestrictionDescription,
            State = RestrictionState.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddAsync(restriction);

        return restriction;
    }

    public async Task<OperationalRestriction> EditRestrictionAsync(
        Guid id,
        OperationalRestrictionDto updateRestrictionDto)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        // TODO: Validate that editing is allowed in the current state.
        restriction.RestrictionDescription =
            updateRestrictionDto.RestrictionDescription;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction> ApproveRestrictionAsync(
        Guid id,
        Guid approverId)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        // TODO: Validate the approval transition and approver authorization.
        restriction.State = RestrictionState.Approved;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction> DenyRestrictionAsync(
        Guid id,
        Guid approverId,
        string reason)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        // TODO: Validate the denial transition and record the reason.
        restriction.State = RestrictionState.CancelledRefused;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction> ArchiveRestrictionAsync(
        Guid id)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        // TODO: Validate that archiving is allowed in the current state.
        restriction.State = RestrictionState.Archived;
        restriction.ArchivedAt = DateTime.UtcNow;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction> AssignOperatorAsync(
        Guid id,
        Guid operatorId)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        // TODO: Validate operator assignment rules.
        restriction.AssignedOperatorsIds.Add(operatorId);

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction?> GetRestrictionByIdAsync(
        Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<List<OperationalRestriction>>
        GetActiveRestrictionsAsync()
    {
        return await _repository.GetAllAsync(
            r => r.State == RestrictionState.Active);
    }

    private async Task<OperationalRestriction> GetRestrictionOrThrowAsync(
        Guid id)
    {
        return await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException(
                $"Restriction with ID '{id}' was not found.");
    }

    private async Task<OperationalRestriction> SaveUpdateAsync(
        OperationalRestriction restriction)
    {
        restriction.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(restriction);

        return restriction;
    }
}
// Gr4 current version<
using Backend.Exceptions;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enum;
using Backend.Models.Requests;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class OperationalRestrictionService(
  AppDbContext appDbContext
) : IOperationalRestrictionService
{
    private readonly AppDbContext _appDbContext = appDbContext;

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
    /// </summary>
    public async Task<OperationalRestrictionDto> GetByIdAsync(Guid id)
    {
        var restriction = await _appDbContext.OperationalRestrictions
          .AsNoTracking()
          .FirstOrDefaultAsync(x => x.Id == id);

        if (restriction == null)
            throw new OperationalRestrictionNotFoundException(id);

        return OperationalRestrictionDto.FromDalEntity(restriction);
    }

    /// <summary>
    /// Retrieves all operational restrictions from the database. Returns an empty list if none are found.
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

        return OperationalRestrictionDto.FromDalEntity(originalRestriction);
    }
}

