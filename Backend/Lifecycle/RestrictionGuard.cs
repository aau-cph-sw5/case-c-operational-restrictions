using Backend.Authorization;
using Backend.Models.DTOs;
using Backend.Models.Enums;
using Backend.Models.Requests;
namespace Backend.Lifecycle;

// Guards for the operational restriction lifecycle
public sealed class StartDateIsFoundGuard : IRestrictionGuard
{
    public string Name => "StartDateIsFound";

    public bool IsSatisfied(object data)
    {
        return data switch
        {
            CreateRestrictionRequest r => r.StartDate.HasValue && r.StartDate.Value != default,
            OperationalRestrictionDto d => d.StartDate != default,
            _ => false
        };
    }
}

public sealed class OperationalRestrictionIdIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "OperationalRestrictionIdIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data.Id != Guid.Empty;
    }
}

public sealed class DescriptionTextIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "DescriptionTextIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrWhiteSpace(data.RestrictionDescription);
    }
}

//To do after Signatures are added
public sealed class OriginatorSignatureIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "OriginatorSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data.OriginatorUserId != Guid.Empty;
    }
}

// A restriction can only be returned for changes when the reviewer is
// recorded and has written what needs to change.
public sealed class ReturnedForChanges : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "ReturnedForChanges";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data.ReviewerId != Guid.Empty
            && !string.IsNullOrWhiteSpace(data.ReviewDescription);
    }
}

// A restriction can only be refused when the reviewer is recorded and has
// written why it is refused.
public sealed class CancelledRefused : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "CancelledRefused";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data.ReviewerId != Guid.Empty
            && !string.IsNullOrWhiteSpace(data.ReviewDescription);
    }
}

//To do after Signatures are added to DTO
public sealed class DutyOperationsManagerSignatureIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "DutyOperationsManagerSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data.Signatures.Any(signature =>
            signature.SigneeRole.Split(", ")
                .Contains(RoleNames.DutyOperationsManager));
    }
}

public sealed class ControlRoomSupervisorSignatureIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "ControlRoomSupervisorSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: Check data.ControlRoomSupervisorSignature when added to DTO
        return true;
    }
}

public sealed class OperatorsAreAssigned : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "OperatorsAreAssigned";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: Check data.AssignedOperatorsIds when added to DTO
        return true;
    }
}

// Cancellation guards. The sequence itself is enforced by the From state of
// each cancellation transition; these guards reserve the signature checks
// once SignatureType is added to the signature DTO (see SignatureType).
public sealed class OriginatorCancellationSignatureIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "OriginatorCancellationSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: Check for a Closure signature with the Originator role when SignatureType is added to the DTO
        return true;
    }
}

public sealed class DutyOperationsManagerCancellationSignatureIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "DutyOperationsManagerCancellationSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: Check for a Closure signature with the DutyOperationsManager role when SignatureType is added to the DTO
        return true;
    }
}

public sealed class ControlRoomCancellationSignatureIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "ControlRoomCancellationSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: Check for a Closure signature with the ControlroomSupervisor role when SignatureType is added to the DTO
        return true;
    }
}
