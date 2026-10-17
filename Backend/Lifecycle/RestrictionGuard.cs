using Backend.Models.DTOs;

namespace Backend.Lifecycle;


/*
"StartDateIsFound", (done)             
"OperationalRestictionIdIsFound" (done)
"DescriptionTextIsFound", (done)
"OriginatorSignatureIsFound", (done)
"RequestedChangesByDom" (done)
"DomRefusesRestriction" (done)
"DomSignatureIsFound", (done)
"OriginatorSignatureIsFound" (done)
"CRSignatureIsFound", (done)
"OperatorsAreAssigned" 
*/

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

public sealed class OriginatorSignatureIsFound : IRestrictionGuard
{
    public string Name => "OriginatorSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.OriginatorSignature); //to add in dto
    }
}

public sealed class RequestedChangesByDom : IRestrictionGuard
{
    public string Name => "RequestedChangesByDom";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.RequestedChangesByDom); //to add in dto
        //
    }
}

public sealed class DomRefusesRestriction : IRestrictionGuard
{
    public string Name => "DomRefusesRestriction        ";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.RefusedRestrictionByDom); //to add in dto
    }
}

public sealed class DomSignatureIsFound : IRestrictionGuard
{
    public string Name => "DomSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.DomSignature); //to add in dto
    }
}

public sealed class OriginatorSignatureIsFound : IRestrictionGuard
{
    public string Name => "OriginatorSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.OriginatorSignature); //to add in dto
    }
}

public sealed class CRSignatureIsFound : IRestrictionGuard
{
    public string Name => "CRSignatureIsFound";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.CRSignature); //to add in dto
    }
}

public sealed class OperatorsAreAssigned : IRestrictionGuard
{
    public string Name => "OperatorsAreAssigned";

    public bool IsSatisfied(OperationalRestrictionDto data)
    {
        return !string.IsNullOrEmpty(data.OperatorsAreAssigned); //to add in dto
    }
}