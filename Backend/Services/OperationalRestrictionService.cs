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

    return await SaveAsNewVersionAsync(restriction, v =>
        v.State = RestrictionState.PendingDutyOperationsManagerApproval);
}

// Versioned audit trail per MET-C-021: edits insert a new version row
// instead of updating in place.
    public async Task<OperationalRestriction> EditRestrictionAsync(
    Guid id,
    OperationalRestrictionDto updateRestrictionDto)
{
    var original = await GetRestrictionOrThrowAsync(id);

    // TODO : restrict editing to Draft/ReturnedForChanges states.
    return await SaveAsNewVersionAsync(original, v =>
    {
        v.RestrictionDescription = updateRestrictionDto.RestrictionDescription;
        v.CreatedBy = updateRestrictionDto.CreatedBy;
    });
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

        return await SaveAsNewVersionAsync(restriction, v =>
            v.State = RestrictionState.Approved);
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

        return await SaveAsNewVersionAsync(restriction, v =>
            v.State = RestrictionState.PendingControlRoomSupervisorApproval);
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

        return await SaveAsNewVersionAsync(restriction, v =>
            v.State = RestrictionState.Active);
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

    return await SaveAsNewVersionAsync(restriction, v =>
    {
        v.State = RestrictionState.CancelledRefused;
        v.ReviewDescription = reason;   // deny reason finally gets stored
    });
}

        public async Task<OperationalRestriction> ArchiveRestrictionAsync(
        Guid id)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        // TODO: Validate that archiving is allowed in the current state.
        return await SaveAsNewVersionAsync(restriction, v =>
        {
            v.State = RestrictionState.Archived;
            v.ArchivedAt = DateTime.UtcNow;
        });
    }

        public async Task<OperationalRestriction> AssignOperatorAsync(
        Guid id,
        Guid operatorId)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        // TODO: Validate operator assignment rules.
        return await SaveAsNewVersionAsync(restriction, v =>
            v.AssignedOperatorsIds.Add(operatorId));
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

        return await SaveAsNewVersionAsync(restriction, v =>
            v.State = RestrictionState.ReturnedForChanges);
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

        return await SaveAsNewVersionAsync(restriction, v =>
            v.State = RestrictionState.Draft);
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



    // Versioned audit trail per MET-C-021: writes insert a new version row
// instead of updating in place.
private async Task<OperationalRestriction> SaveAsNewVersionAsync(
    OperationalRestriction original,
    Action<OperationalRestriction> applyChanges)
{
    var now = DateTime.UtcNow;

    var newVersion = new OperationalRestriction
    {
        RestrictionId = Guid.NewGuid(),
        OriginatorUserId = original.OriginatorUserId,
        CreatedBy = original.CreatedBy,
        SignatureSheet = new List<Signature>(original.SignatureSheet),
        State = original.State,
        StartDate = original.StartDate,
        EndDate = original.EndDate,
        CreatedAt = now,
        ArchivedAt = original.ArchivedAt,
        RetentionUntil = original.RetentionUntil,
        RestrictionDescription = original.RestrictionDescription,
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

    applyChanges(newVersion);

    await _repository.AddAsync(newVersion);

    return newVersion;
}

}

