using Backend.Models.DTOs;
using Backend.Models.Enums;

namespace Backend.Lifecycle;

public interface IRestrictionGuard
{
    string Name { get; }

    bool IsSatisfied(OperationalRestrictionDto data);
}


// Guards for the operational restriction lifecycle
public sealed class StartDateIsFoundGuard : IRestrictionGuard
{
    public string Name => "StartDateIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data is not null
            && data.StartDate != default;
    }
}

public sealed class OperationalRestictionIdIsFound : IRestrictionGuard
{
    public string Name => "OperationalRestictionIdIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data.RestrictionId != Guid.Empty;
    }
}

public sealed class DescriptionTextIsFound : IRestrictionGuard
{
    public string Name => "DescriptionTextIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.RestrictionDescription);
    }
}

//To do after Signatures are added
public sealed class OriginatorSignatureIsFound : IRestrictionGuard
{
    public string Name => "OriginatorSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return data.OriginatorUserId != Guid.Empty;
    }
}

public sealed class ReturnedForChanges : IRestrictionGuard
{
    public string Name => "ReturnedForChanges";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        data.State = RestrictionState.PendingDutyOperationsManagerApproval;
        return true;
    }   
}

public sealed class CancelledRefused : IRestrictionGuard
{
    public string Name => "CancelledRefused";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        data.State = RestrictionState.PendingDutyOperationsManagerApproval;
        return true; 
    }
}
/*
//To do after Signatures are added
public sealed class DutyOperationsManagerSignatureIsFound : IRestrictionGuard
{
    public string Name => "DutyOperationsManagerSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.DutyOperationsManagerSignature); //to add in dto
    }
}


//To do after Signatures are added
public sealed class OriginatorSignatureIsFound : IRestrictionGuard
{
    public string Name => "OriginatorSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.OriginatorSignature); //to add in dto
    }
}

//To do after Signatures are added
public sealed class ControlRoomSupervisorSignatureIsFound : IRestrictionGuard
{
    public string Name => "ControlRoomSupervisorignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.ControlRoomSupervisorSignature); //to add in dto
    }
}

public sealed class OperatorsAreAssigned : IRestrictionGuard
{
    public string Name => "OperatorsAreAssigned";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.OperatorsAreAssigned); //to add in dto
    }
} */