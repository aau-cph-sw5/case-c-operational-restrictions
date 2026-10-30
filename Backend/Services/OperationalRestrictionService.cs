using Backend.Data;
using Backend.Exceptions;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;
using Backend.Models.Requests;

namespace Backend.Services;

public class OperationalRestrictionService
    : IOperationalRestrictionService
{
    private readonly RestrictionLifecycleService _lifecycleService;
    private readonly IOperationalRestrictionRepository _repository;
    private readonly IRestrictionTransitionEventRepository _transitionEventRepository;

    public OperationalRestrictionService(
        RestrictionLifecycleService lifecycleService,
        IOperationalRestrictionRepository repository,
        IRestrictionTransitionEventRepository transitionEventRepository)
    {
        _lifecycleService = lifecycleService;
        _repository = repository;
        _transitionEventRepository = transitionEventRepository;
    }

    public async Task<OperationalRestrictionDto> CreateRestrictionAsync(
        CreateRestrictionRequest data)
    {
        if (data.EndDate <= data.StartDate)
            throw new InvalidRestrictionPeriodException(data.StartDate, data.EndDate);

        var initialState = _lifecycleService.EnsureCanCreate(data);

        var now = DateTime.UtcNow;

        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = data.CreatedBy,
            CreatedBy = data.CreatedBy,
            StartDate = data.StartDate,
            EndDate = data.EndDate,
            RestrictionDescription = data.RestrictionDescription,
            State = initialState,
            CreatedAt = now,
            UpdatedAt = now,
            OriginalOperationalRestrictionId = null
        };

        await _repository.AddAsync(restriction);

        await RecordTransitionAsync(
            restriction, RestrictionAction.CreateDraft, null, data.CreatedBy);

        return OperationalRestrictionDto.FromOperationalRestrictionToDto(restriction);
    }

    public Task<OperationalRestriction> SubmitForApprovalAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(
            id, RestrictionAction.PendingDutyOperationsManagerApproval, actorId);
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

    public Task<OperationalRestriction> ApproveRestrictionAsync(
        Guid id,
        Guid approverId)
    {
        return TransitionAsync(id, RestrictionAction.Approved, approverId);
    }

    public Task<OperationalRestriction> SubmitForControlRoomSupervisorApprovalAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(
            id, RestrictionAction.PendingControlRoomSupervisorApproval, actorId);
    }

    public Task<OperationalRestriction> ActivateRestrictionAsync(
        Guid id,
        Guid approverId)
    {
        return TransitionAsync(id, RestrictionAction.Active, approverId);
    }

    public Task<OperationalRestriction> DenyRestrictionAsync(
        Guid id,
        Guid approverId,
        string reason)
    {
        return TransitionAsync(
            id,
            RestrictionAction.CancelledRefused,
            approverId,
            v =>
            {
                v.ReviewerId = approverId;
                v.ReviewDescription = reason;
            });
    }

    public Task<OperationalRestriction> ReturnForChangesAsync(
        Guid id,
        Guid reviewerId,
        string reason)
    {
        return TransitionAsync(
            id,
            RestrictionAction.ReturnedForChanges,
            reviewerId,
            v =>
            {
                v.ReviewerId = reviewerId;
                v.ReviewDescription = reason;
            });
    }

    public Task<OperationalRestriction> ResubmitRestrictionAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(id, RestrictionAction.Resubmit, actorId);
    }

    // Cancellation transitions (MET-C-018): the originator starts a
    // cancellation of an Active restriction, duty operations manager and
    // control room approve it in sequence, and the operators archive it.
    // Signature guards are enforced by the lifecycle; the signature data
    // itself is recorded once SignatureType is added to the DTO.

    public Task<OperationalRestriction> StartCancellationAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(
            id, RestrictionAction.OriginatorStartCancellation, actorId);
    }

    public Task<OperationalRestriction> ApproveCancellationAsync(
        Guid id,
        Guid approverId)
    {
        return TransitionAsync(
            id, RestrictionAction.DutyOperationsManagerApproveCancellation, approverId);
    }

    public Task<OperationalRestriction> ControlRoomCancellationAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(
            id, RestrictionAction.ControlRoomCancellation, actorId);
    }

    public Task<OperationalRestriction> OperatorsArchiveAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(id, RestrictionAction.OperatorsArchive, actorId);
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

    // Single entry point for lifecycle transitions (MET-C-002): the target state
    // comes from RestrictionLifecycle, guards are evaluated against the version
    // the transition would produce (stored data plus the changes it applies),
    // and every transition is recorded as an audit event.
    private async Task<OperationalRestriction> TransitionAsync(
        Guid id,
        RestrictionAction action,
        Guid actorId,
        Action<OperationalRestriction>? applyChanges = null)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        await EnsureIsLatestVersionAsync(restriction);

        var nextState = _lifecycleService.GetNextState(restriction.State, action);

        var newVersion = CreateNextVersion(restriction, v =>
        {
            v.State = nextState;
            v.CreatedBy = actorId;
            applyChanges?.Invoke(v);
        });

        _lifecycleService.EnsureCanTransition(
            restriction.State,
            action,
            OperationalRestrictionDto.FromOperationalRestrictionToDto(newVersion));

        await _repository.AddAsync(newVersion);

        await RecordTransitionAsync(newVersion, action, restriction.State, actorId);

        return newVersion;
    }

    // Transitioning an older version would fork the history, so only the
    // newest version of a restriction may move.
    private async Task EnsureIsLatestVersionAsync(OperationalRestriction restriction)
    {
        var originalId = restriction.OriginalOperationalRestrictionId ?? restriction.RestrictionId;

        var versions = await _repository.GetAllAsync(
            r => r.RestrictionId == originalId
                || r.OriginalOperationalRestrictionId == originalId);

        if (versions.Any(v => v.Version > restriction.Version))
        {
            throw new InvalidOperationException(
                $"Restriction version '{restriction.RestrictionId}' is outdated. " +
                "Only the latest version can be transitioned.");
        }
    }

    private Task RecordTransitionAsync(
        OperationalRestriction version,
        RestrictionAction action,
        RestrictionState? fromState,
        Guid actorId)
    {
        return _transitionEventRepository.AddAsync(new RestrictionTransitionEvent
        {
            Id = Guid.NewGuid(),
            RestrictionVersionId = version.RestrictionId,
            OriginalRestrictionId = version.OriginalOperationalRestrictionId ?? version.RestrictionId,
            Action = action,
            FromState = fromState,
            ToState = version.State,
            ActorId = actorId,
            OccurredAt = DateTime.UtcNow
        });
    }



    // Versioned audit trail per MET-C-021: writes insert a new version row
    // instead of updating in place.
    private async Task<OperationalRestriction> SaveAsNewVersionAsync(
        OperationalRestriction original,
        Action<OperationalRestriction> applyChanges)
    {
        var newVersion = CreateNextVersion(original, applyChanges);

        await _repository.AddAsync(newVersion);

        return newVersion;
    }

    private static OperationalRestriction CreateNextVersion(
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

        return newVersion;
    }

}
