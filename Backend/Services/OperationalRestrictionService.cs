using Backend.Data;
using Backend.Lifecycle;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;

namespace Backend.Services;

//TODO: This interface should be seperated from the class
public interface IOperationalRestrictionService
{
    OperationalRestriction CreateRestriction(OperationalRestrictionDto data);

    OperationalRestriction EditRestriction(
        Guid id,
        OperationalRestrictionDto updateRestrictionDto);

    OperationalRestriction ApproveRestriction(
        Guid id,
        Guid approverId);

    OperationalRestriction DenyRestriction(
        Guid id,
        Guid approverId,
        string reason);

    OperationalRestriction ArchiveRestriction(Guid id);

    OperationalRestriction AssignOperator(
        Guid id,
        Guid operatorId);

    OperationalRestriction? GetRestrictionById(Guid id);

    List<OperationalRestriction> GetActiveRestrictions();
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

    public OperationalRestriction CreateRestriction(
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

        _repository.Add(restriction);

        return restriction;
    }

    public OperationalRestriction EditRestriction(
        Guid id,
        OperationalRestrictionDto updateRestrictionDto)
    {
        var restriction = GetRestrictionOrThrow(id);

        // TODO: Validate that editing is allowed in the current state.
        restriction.RestrictionDescription =
            updateRestrictionDto.RestrictionDescription;

        return SaveUpdate(restriction);
    }

    public OperationalRestriction ApproveRestriction(
        Guid id,
        Guid approverId)
    {
        var restriction = GetRestrictionOrThrow(id);

        // TODO: Validate the approval transition and approver authorization.
        restriction.State = RestrictionState.Approved;

        return SaveUpdate(restriction);
    }

    public OperationalRestriction DenyRestriction(
        Guid id,
        Guid approverId,
        string reason)
    {
        var restriction = GetRestrictionOrThrow(id);

        // TODO: Validate the denial transition and record the reason.
        restriction.State = RestrictionState.CancelledRefused;

        return SaveUpdate(restriction);
    }

    public OperationalRestriction ArchiveRestriction(Guid id)
    {
        var restriction = GetRestrictionOrThrow(id);

        // TODO: Validate that archiving is allowed in the current state.
        restriction.State = RestrictionState.Archived;
        restriction.ArchivedAt = DateTime.UtcNow;

        return SaveUpdate(restriction);
    }

    public OperationalRestriction AssignOperator(
        Guid id,
        Guid operatorId)
    {
        var restriction = GetRestrictionOrThrow(id);

        // TODO: Validate operator assignment rules.
        restriction.AssignedOperatorsIds.Add(operatorId);

        return SaveUpdate(restriction);
    }

    public OperationalRestriction? GetRestrictionById(Guid id)
    {
        return _repository.GetById(id);
    }

    public List<OperationalRestriction> GetActiveRestrictions()
    {
        return _repository.GetAll(
            r => r.State == RestrictionState.Active);
    }

    private OperationalRestriction GetRestrictionOrThrow(Guid id)
    {
        return _repository.GetById(id)
            ?? throw new InvalidOperationException(
                $"Restriction with ID '{id}' was not found.");
    }

    private OperationalRestriction SaveUpdate(
        OperationalRestriction restriction)
    {
        restriction.UpdatedAt = DateTime.UtcNow;

        _repository.Update(restriction);

        return restriction;
    }
}
