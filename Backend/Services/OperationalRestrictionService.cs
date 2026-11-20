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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignatureService _signatureService;

    public OperationalRestrictionService(
        RestrictionLifecycleService lifecycleService,
        IOperationalRestrictionRepository repository,
        IRestrictionTransitionEventRepository transitionEventRepository,
        IUnitOfWork unitOfWork,
        ISignatureService signatureService)
    {
        _lifecycleService = lifecycleService;
        _repository = repository;
        _transitionEventRepository = transitionEventRepository;
        _unitOfWork = unitOfWork;
        _signatureService = signatureService
            ?? throw new ArgumentNullException(nameof(signatureService));
    }

    /// <summary>
    /// Creates the first version of a new operational restriction as a draft.
    /// The creator is also recorded as the originator.
    /// </summary>
    public async Task<OperationalRestrictionDto> CreateRestrictionAsync(CreateRestrictionRequest data)
    {
        if (string.IsNullOrWhiteSpace(data.Title))
            throw new MissingRequiredRestrictionFieldException(nameof(data.Title));

        if (!data.StartDate.HasValue || data.StartDate.Value == default)
            throw new MissingRequiredRestrictionFieldException(nameof(data.StartDate));

        if (string.IsNullOrWhiteSpace(data.SituationDescription))
            throw new MissingRequiredRestrictionFieldException(nameof(data.SituationDescription));

        if (string.IsNullOrWhiteSpace(data.RestrictionDescription))
            throw new MissingRequiredRestrictionFieldException(nameof(data.RestrictionDescription));

        if (string.IsNullOrWhiteSpace(data.CancellationDescription))
            throw new MissingRequiredRestrictionFieldException(nameof(data.CancellationDescription));

        var startUtc = ToUtc(data.StartDate.Value);
        DateTime? endUtc = data.EndDate.HasValue ? ToUtc(data.EndDate.Value) : null;

        if (endUtc.HasValue && endUtc.Value <= startUtc)
            throw new InvalidRestrictionPeriodException(startUtc, endUtc.Value);

        var initialState = _lifecycleService.EnsureCanCreate(data);

        var now = DateTime.UtcNow;

        var restriction = new OperationalRestriction
        {
            RestrictionId = Guid.NewGuid(),
            OriginatorUserId = data.CreatedBy,
            CreatedBy = data.CreatedBy,
            Title = data.Title,
            Reference = data.Reference,
            SituationDescription = data.SituationDescription,
            RestrictionDescription = data.RestrictionDescription,
            CancellationDescription = data.CancellationDescription,
            State = initialState,
            StartDate = startUtc,
            EndDate = endUtc,
            CreatedAt = now,
            UpdatedAt = now,
            OriginalOperationalRestrictionId = null
        };

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        await _repository.AddAsync(restriction);

        await RecordTransitionAsync(
            restriction, RestrictionAction.CreateDraft, null, data.CreatedBy);

        await transaction.CommitAsync();

        return OperationalRestrictionDto.FromOperationalRestrictionToDto(restriction);
    }

    /// <summary>
    /// Retrieves an operational restriction by its ID. Throws an exception if not found.
    /// Drafts are private to their originator, so another user's draft is treated as not found.
    /// Resolves to the latest version of the restriction if newer versions exist.
    /// </summary>
    public async Task<OperationalRestrictionDto> GetByIdAsync(Guid id, Guid currentUserId)
    {
        var restriction = await GetLatestRestrictionOrThrowAsync(id);

        if (restriction.State == RestrictionState.Draft && restriction.OriginatorUserId != currentUserId)
            throw new OperationalRestrictionNotFoundException(id);

        return await CreateDtoWithHistoryAsync(restriction);
    }

    public async Task<OperationalRestrictionDto> GetByIdAsync(Guid id)
    {
        var restriction = await GetLatestRestrictionOrThrowAsync(id);

        return await CreateDtoWithHistoryAsync(restriction);
    }

    private async Task<OperationalRestrictionDto> CreateDtoWithHistoryAsync(
        OperationalRestriction restriction)
    {
        var dto = OperationalRestrictionDto.FromOperationalRestrictionToDto(restriction);
        var originatorSignature = _signatureService.GetOriginatorSignature(restriction.RestrictionId);
        if (originatorSignature != null && dto.OriginatorSignature == null)
            dto.OriginatorSignature = SignatureDto.FromDalEntity(originatorSignature);

        var originalId = restriction.OriginalOperationalRestrictionId ?? restriction.RestrictionId;
        var events = await _transitionEventRepository.GetByOriginalRestrictionIdAsync(originalId);
        dto.History = events.Select(transitionEvent => new RestrictionTransitionEventDto
        {
            Action = transitionEvent.Action.ToString(),
            FromState = transitionEvent.FromState?.ToString(),
            ToState = transitionEvent.ToState.ToString(),
            ActorId = transitionEvent.ActorId,
            OccurredAt = transitionEvent.OccurredAt
        }).ToList();

        return dto;
    }

    private async Task<OperationalRestriction> GetLatestRestrictionOrThrowAsync(Guid id)
    {
        var restriction = await _repository.GetByIdAsync(id)
            ?? throw new OperationalRestrictionNotFoundException(id);

        var originalId = restriction.OriginalOperationalRestrictionId ?? restriction.RestrictionId;
        var versions = await _repository.GetAllAsync(
            r => r.RestrictionId == originalId || r.OriginalOperationalRestrictionId == originalId);

        var latestVersion = versions.OrderByDescending(v => v.Version).FirstOrDefault();
        if (latestVersion != null && latestVersion.RestrictionId != restriction.RestrictionId)
        {
            restriction = await _repository.GetByIdAsync(latestVersion.RestrictionId) ?? latestVersion;
        }

        return restriction;
    }

    public async Task<Signature> SignOriginatorAndAdvanceToPendingApprovalAsync(Guid id, Guid signerId)
    {
        var restriction = await _repository.GetByIdAsync(id)
            ?? throw new OperationalRestrictionNotFoundException(id);

        if (restriction.State != RestrictionState.Draft)
            throw new OperationalRestrictionNotSignableException(id, restriction.State.ToString());

        Signature? signature = null;
        try
        {
            signature = _signatureService.SignOriginator(id, signerId);

            await TransitionAsync(
                id,
                RestrictionAction.PendingDutyOperationsManagerApproval,
                signerId,
                newVersion =>
                {
                    newVersion.SignatureSheet.Add(signature);
                });

            return signature;
        }
        catch
        {
            if (signature != null)
                _signatureService.DiscardUnpersistedSignature(signature);

            throw;
        }
    }


    /// <summary>
    /// The control room supervisor signs the restriction for implementation, which moves it from PendingControlRoomSupervisorApproval to Active.
    /// Only users with the ControlroomSupervisor role may sign.
    /// </summary>
    public async Task<Signature> SignControlRoomSupervisorApprovalAsync(Guid id, Guid signerId)
    {
        var signatureService = _signatureService;

        var restriction = await _repository.GetByIdAsync(id)
            ?? throw new OperationalRestrictionNotFoundException(id);

        if (restriction.State != RestrictionState.PendingControlRoomSupervisorApproval)
            throw new OperationalRestrictionNotSignableException(id, restriction.State.ToString());

        Signature? signature = null;
        try
        {
            signature = signatureService.SignControlRoomSupervisor(id, signerId);
            await TransitionAsync(
                id,
                RestrictionAction.Active,
                signerId,
                newVersion =>
                {
                    newVersion.SignatureSheet.Add(signature);

                    newVersion.AssignedOperatorsIds =
                        signatureService.GetAtWorkAndOffWorkUserIds();
                });

            return signature;
        }
        catch
        {
            if (signature != null)
                signatureService.DiscardUnpersistedSignature(signature);

            throw;
        }
    }

    /// <summary>
    /// Retrieves all operational restrictions from the database. Returns an empty list if none are found.
    /// Drafts are only included for their originator.
    /// </summary>
    public async Task<List<OperationalRestrictionDto>> GetAllAsync(Guid currentUserId)
    {
        var restrictions = await _repository.GetAllAsync(
            x => x.State != RestrictionState.Draft || x.OriginatorUserId == currentUserId);

        return restrictions
            .Select(OperationalRestrictionDto.FromOperationalRestrictionToDto)
            .ToList();
    }

    public Task<OperationalRestrictionDto> SubmitForApprovalAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(
            id, RestrictionAction.PendingDutyOperationsManagerApproval, actorId);
    }

    // Versioned audit trail per MET-C-021: edits insert a new version row
    // instead of updating in place.
    public async Task<OperationalRestrictionDto> EditRestrictionAsync(
        Guid id,
        OperationalRestrictionDto updateRestrictionDto)
    {
        var original = await GetRestrictionOrThrowAsync(id);

        // TODO : restrict editing to Draft/ReturnedForChanges states.
        var newVersion = await SaveAsNewVersionAsync(original, v =>
        {
            v.RestrictionDescription = updateRestrictionDto.RestrictionDescription;
            v.CreatedBy = updateRestrictionDto.CreatedBy;
        });

        return OperationalRestrictionDto.FromOperationalRestrictionToDto(newVersion);
    }

    public async Task<OperationalRestrictionDto> ApproveRestrictionAsync(
        Guid id,
        Guid approverId)
    {
        var restriction = await GetRestrictionOrThrowAsync(id);

        // Separation of duties: even with an approving role, the originator
        // of a restriction must not approve it themselves.
        if (restriction.OriginatorUserId == approverId)
            throw new SelfApprovalNotAllowedException(id, approverId);

        var signature = _signatureService.SignDutyOperationsManager(id, approverId);
        OperationalRestrictionDto approved;
        try
        {
            approved = await TransitionAsync(
                id,
                RestrictionAction.Approved,
                approverId,
                version => version.SignatureSheet.Add(signature));
        }
        catch
        {
            _signatureService.DiscardUnpersistedSignature(signature);
            throw;
        }

        return await TransitionAsync(
            approved.Id,
            RestrictionAction.PendingControlRoomSupervisorApproval,
            approverId);
    }

    public Task<OperationalRestrictionDto> SubmitForControlRoomSupervisorApprovalAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(
            id, RestrictionAction.PendingControlRoomSupervisorApproval, actorId);
    }

    public Task<OperationalRestrictionDto> ActivateRestrictionAsync(
        Guid id,
        Guid approverId)
    {
        return TransitionAsync(id, RestrictionAction.Active, approverId);
    }

    public Task<OperationalRestrictionDto> DenyRestrictionAsync(
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

    public Task<OperationalRestrictionDto> ReturnForChangesAsync(
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

    public Task<OperationalRestrictionDto> ResubmitRestrictionAsync(
        Guid id,
        Guid actorId)
    {
        return TransitionAsync(id, RestrictionAction.Resubmit, actorId);
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

    public async Task<List<OperationalRestrictionDto>> GetActiveRestrictionsAsync()
    {
        var restrictions = await _repository.GetAllAsync(
            r => r.State == RestrictionState.Active);

        return restrictions
            .Select(OperationalRestrictionDto.FromOperationalRestrictionToDto)
            .ToList();
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
    private async Task<OperationalRestrictionDto> TransitionAsync(
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

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        await _repository.AddAsync(newVersion);

        await RecordTransitionAsync(newVersion, action, restriction.State, actorId);

        await transaction.CommitAsync();

        return await CreateDtoWithHistoryAsync(newVersion);
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
        var newRestrictionId = Guid.NewGuid();

        // Signatures and files are one-to-many child rows, so each version gets
        // its own copies; reusing the original instances would re-insert them
        // with their existing keys.
        var newVersion = new OperationalRestriction
        {
            RestrictionId = newRestrictionId,
            OriginatorUserId = original.OriginatorUserId,
            CreatedBy = original.CreatedBy,
            Title = original.Title,
            Reference = original.Reference,
            SituationDescription = original.SituationDescription,
            RestrictionDescription = original.RestrictionDescription,
            CancellationDescription = original.CancellationDescription,
            SignatureSheet = original.SignatureSheet
                .Select(s => new Signature
                {
                    Id = Guid.NewGuid(),
                    RequiresSignatureId = s.RequiresSignatureId,
                    OperationalRestrictionId = newRestrictionId,
                    SigneeId = s.SigneeId,
                    SigneeRole = s.SigneeRole,
                    SignedAt = s.SignedAt
                })
                .ToList(),
            State = original.State,
            StartDate = original.StartDate,
            EndDate = original.EndDate,
            CreatedAt = now,
            ArchivedAt = original.ArchivedAt,
            RetentionUntil = original.RetentionUntil,
            Files = original.Files
                .Select(f => new Backend.Models.Entities.File
                {
                    FileId = Guid.NewGuid(),
                    FileName = f.FileName,
                    StoragePath = f.StoragePath,
                    RestrictionId = newRestrictionId,
                    CreatedAt = f.CreatedAt
                })
                .ToList(),
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

    private static DateTime ToUtc(DateTime dateTime) =>
        dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
}
