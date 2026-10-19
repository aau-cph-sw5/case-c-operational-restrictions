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

    Task<OperationalRestriction> SubmitForApprovalAsync(
        Guid id,
        OperationalRestrictionDto data);

    Task<OperationalRestriction> ApproveRestrictionAsync(
        Guid id,
        Guid approverId,
        OperationalRestrictionDto data);

    Task<OperationalRestriction> SubmitForControlRoomSupervisorApprovalAsync(
        Guid id,
        OperationalRestrictionDto data);

    Task<OperationalRestriction> ActivateRestrictionAsync(
        Guid id,
        Guid approverId,
        OperationalRestrictionDto data);

    Task<OperationalRestriction> DenyRestrictionAsync(
        Guid id,
        Guid approverId,
        string reason,
        OperationalRestrictionDto data);

    Task<OperationalRestriction> ArchiveRestrictionAsync(Guid id);

    Task<OperationalRestriction> AssignOperatorAsync(
        Guid id,
        Guid operatorId);

    Task<OperationalRestriction> ReturnForChangesAsync(
        Guid id,
        OperationalRestrictionDto data);

    Task<OperationalRestriction> ResubmitRestrictionAsync(
        Guid id,
        OperationalRestrictionDto data);

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