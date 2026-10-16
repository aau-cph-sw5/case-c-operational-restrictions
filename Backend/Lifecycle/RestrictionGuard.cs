using Backend.Models.DTOs;
using Backend.Models.Entities;

namespace Backend.Lifecycle;

public interface IRestrictionGuard
{
    string Name { get; }

    bool IsSatisfied(OperationalRestriction data);
}

public sealed class CreationDataIsValidGuard : IRestrictionGuard
{
    public string Name => "CreationDataIsValid";

    public bool IsSatisfied(OperationalRestriction data)
    {
        return data is not null
            && !string.IsNullOrWhiteSpace(data.RestrictionDescription);
    }
}

// missing guards from draft to active

// TODO: Find signatures and check if they are signed
public sealed class OriginatorSignatureFoundGuard : IRestrictionGuard
{
    public string Name => "OriginatorSignatureFound";

    public bool IsSatisfied(OperationalRestriction data)
    {
        return data is not null; // and find originator cancellation signature
    
    }
}

public sealed class DOMCancelSignatureFoundGuard : IRestrictionGuard
{
    public string Name => "DOMSignatureFound";

    public bool IsSatisfied(OperationalRestriction data)
    {
        return data is not null; // and find DOM cancellation signature
    }
}

public sealed class ControlRoomSignatureFoundGuard : IRestrictionGuard
{
    public string Name => "ControlRoomSignatureFound";

    public bool IsSatisfied(OperationalRestriction data)
    {
        return data is not null; // and find control room cancellation signature
    }
}

public sealed class OperatorSignoutsFoundGuard : IRestrictionGuard
{
    public string Name => "OperatorSignoutsFound";

    public bool IsSatisfied(OperationalRestriction data)
    {
        return data is not null; // and find all operator cancellation signatures
    }
}
