using Backend.Models.DTOs;
using Backend.Models.Enums;
using Backend.Models.Requests;
namespace Backend.Lifecycle;

public interface IRestrictionGuard
{
    string Name { get; }

    bool IsSatisfied(object data);
}

public interface IRestrictionGuard<T> : IRestrictionGuard
{
    bool IsSatisfied(T data);

    bool IRestrictionGuard.IsSatisfied(object data)
    {
        if (data is T specificData)
        {
            return IsSatisfied(specificData);
        }
        return false;
    }
}


// Guards for the operational restriction lifecycle
public sealed class StartDateIsFoundGuard : IRestrictionGuard
{
    public string Name => "StartDateIsFound";

    public bool IsSatisfied(object data)
    {
        return data switch
        {
            CreateRestrictionRequest r => r.StartDate != default,
            OperationalRestrictionDto d => d.StartDate != default,
            _ => false
        };
    }
}

public sealed class OperationalRestictionIdIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "OperationalRestictionIdIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data.RestrictionId != Guid.Empty;
    }
}

public sealed class DescriptionTextIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "DescriptionTextIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.RestrictionDescription);
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

public sealed class ReturnedForChanges : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "ReturnedForChanges";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        data.State = RestrictionState.PendingDutyOperationsManagerApproval;
        return true;
    }   
}

public sealed class CancelledRefused : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "CancelledRefused";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        data.State = RestrictionState.PendingDutyOperationsManagerApproval;
        return true; 
    }
}
//To do after Signatures are added to DTO
public sealed class DutyOperationsManagerSignatureIsFound : IRestrictionGuard<OperationalRestrictionDto>
{
    public string Name => "DutyOperationsManagerSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: Check data.DutyOperationsManagerSignature when added to DTO
        return true;
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

// Cancellation guards - group 1:

public sealed class OriginatorCancellationSignatureIsFound : IRestrictionGuard
{
    public string Name => "OriginatorCancellationSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: check data.SignatureSheet.OriginatorCancellationSignature when Dto and SignatureType is added
        return true;
    
    }
}

public sealed class DutyOperationsManagerCancellationSignatureIsFound : IRestrictionGuard
{
    public string Name => "DutyOperationsManagerCancellationSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: check data.SignatureSheet.DutyOperationsManagerCancellationSignature when Dto and SignatureType is added
        return true;
    }
}

public sealed class ControlRoomCancellationSignatureIsFound : IRestrictionGuard
{
    public string Name => "ControlRoomCancellationSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: check data.SignatureSheet.ControlRoomCancellationSignature when Dto and SignatureType is added
        return true;
    }
}

public sealed class OperatorCancellationSignaturesAreFound : IRestrictionGuard
{
    public string Name => "OperatorCancellationSignaturesAreFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        // TODO: check data.SignatureSheet for all OperatorCancellationSignature when Dto and SignatureType is added
        return true;
    }
}