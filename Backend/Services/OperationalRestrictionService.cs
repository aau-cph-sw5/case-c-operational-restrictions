using Backend.Exceptions;
using Backend.Data;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;

namespace Backend.Services;


//TODO: This interface should be seperated from the class

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

    public async Task<OperationalRestrictionDto> CreateRestrictionAsync(
        CreateRestrictionRequest data)
    {
        if (data.EndDate <= data.StartDate)
            throw new InvalidRestrictionPeriodException(data.StartDate, data.EndDate);

        _lifecycleService.EnsureCanCreate(data);

        var now = DateTime.UtcNow;

        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = data.CreatedBy,
            CreatedBy = data.CreatedBy,
            StartDate = data.StartDate,
            EndDate = data.EndDate,
            RestrictionDescription = data.RestrictionDescription,
            State = RestrictionState.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            OriginalOperationalRestrictionId = null
        };

        await _repository.AddAsync(restriction);

        return OperationalRestrictionDto.FromOperationalRestrictionToDto(restriction);
    }

    public async Task<OperationalRestriction> SubmitForApprovalAsync(
        Guid id,
        OperationalRestrictionDto data)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        _lifecycleService.EnsureCanTransition(
            restriction.State,
            RestrictionAction.PendingDutyOperationsManagerApproval,
            data);

        restriction.State = RestrictionState.PendingDutyOperationsManagerApproval;

        return await SaveUpdateAsync(restriction);
    }

// Versioned audit trail per MET-C-021: edits insert a new version row
// instead of updating in place.
    public async Task<OperationalRestriction> EditRestrictionAsync(
    Guid id,
    OperationalRestrictionDto updateRestrictionDto)
{
    var original = await GetRestrictionOrThrowAsync(id);

    // TODO : restrict editing to Draft/ReturnedForChanges states.
    var now = DateTime.UtcNow;

    var editedVersion = new OperationalRestriction
    {
        RestrictionId = Guid.NewGuid(),
        OriginatorUserId = original.OriginatorUserId,
        CreatedBy = updateRestrictionDto.CreatedBy,
        SignatureSheet = new List<Signature>(original.SignatureSheet),
        State = original.State,
        StartDate = original.StartDate,
        EndDate = original.EndDate,
        CreatedAt = now,
        ArchivedAt = original.ArchivedAt,
        RetentionUntil = original.RetentionUntil,
        RestrictionDescription = updateRestrictionDto.RestrictionDescription,
        Files = new List<Backend.Models.Entities.File>(original.Files),
        Version = original.Version + 1,
        ReviewerId = original.ReviewerId,
        ApprovedForSigning = original.ApprovedForSigning,
        ReviewDescription = original.ReviewDescription,
        AssignedOperatorsIds = new List<Guid>(original.AssignedOperatorsIds),
        UpdatedAt = now,
        OriginalOperationalRestrictionId =
            original.OriginalOperationalRestrictionId ?? original.RestrictionId
    };

    await _repository.AddAsync(editedVersion);

    return editedVersion;
}

    public async Task<OperationalRestriction> ApproveRestrictionAsync(
        Guid id,
        Guid approverId,
        OperationalRestrictionDto data)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        _lifecycleService.EnsureCanTransition(
            restriction.State,
            RestrictionAction.Approved,
            data);

        restriction.State = RestrictionState.Approved;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction> SubmitForControlRoomSupervisorApprovalAsync(
        Guid id,
        OperationalRestrictionDto data)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        _lifecycleService.EnsureCanTransition(
            restriction.State,
            RestrictionAction.PendingControlRoomSupervisorApproval,
            data);

        restriction.State = RestrictionState.PendingControlRoomSupervisorApproval;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction> ActivateRestrictionAsync(
        Guid id,
        Guid approverId,
        OperationalRestrictionDto data)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        _lifecycleService.EnsureCanTransition(
            restriction.State,
            RestrictionAction.Active,
            data);

        restriction.State = RestrictionState.Active;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction> DenyRestrictionAsync(
        Guid id,
        Guid approverId,
        string reason,
        OperationalRestrictionDto data)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        _lifecycleService.EnsureCanTransition(
            restriction.State,
            RestrictionAction.CancelledRefused,
            data);

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

    public async Task<OperationalRestriction> ReturnForChangesAsync(
        Guid id,
        OperationalRestrictionDto data)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        _lifecycleService.EnsureCanTransition(
            restriction.State,
            RestrictionAction.ReturnedForChanges,
            data);

        restriction.State = RestrictionState.ReturnedForChanges;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestriction> ResubmitRestrictionAsync(
        Guid id,
        OperationalRestrictionDto data)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        _lifecycleService.EnsureCanTransition(
            restriction.State,
            RestrictionAction.Resubmit,
            data);

        restriction.State = RestrictionState.Draft;

        return await SaveUpdateAsync(restriction);
    }

    public async Task<OperationalRestrictionDto> GetRestrictionByIdAsync(Guid id)
{
    var restriction = await _repository.GetByIdAsync(id)
        ?? throw new OperationalRestrictionNotFoundException(id);

    return OperationalRestrictionDto.FromOperationalRestrictionToDto(restriction);
}

public async Task<List<OperationalRestrictionDto>> GetAllAsync()
{
    var restrictions = await _repository.GetAllAsync();
    return restrictions
        .Select(OperationalRestrictionDto.FromOperationalRestrictionToDto)
        .ToList();
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

/*
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
*/

