namespace Backend.Exceptions;

public class OperationalRestrictionNotFoundException : Exception
{
    public OperationalRestrictionNotFoundException(Guid id) 
        : base($"Operational restriction was not found {id}")
    {
    }
}

public class RequiresSignatureNotFoundException : Exception
{
    public RequiresSignatureNotFoundException(Guid id)
        : base($"Required signature was not found {id}")
    {
    }
}

public class InvalidRestrictionPeriodException : Exception
{
    public InvalidRestrictionPeriodException(DateTime startDate, DateTime endDate)
        : base($"End date {endDate:O} must be after start date {startDate:O}")
    {
    }
}
public class SignatureNotFoundException : Exception
{
    public SignatureNotFoundException(Guid id)
        : base($"Signature was not found {id}")
    {
    }
}

public class NotRequiredSignerException : Exception
{
    public NotRequiredSignerException(Guid userId, Guid requiresSignatureId)
        : base($"User {userId} is not the required signer of required signature {requiresSignatureId}")
    {
    }
}

public class RequiresSignatureNotSignableException : Exception
{
    public RequiresSignatureNotSignableException(Guid requiresSignatureId, string status)
        : base($"Required signature {requiresSignatureId} cannot be signed because it is {status}")
    {
    }
}
