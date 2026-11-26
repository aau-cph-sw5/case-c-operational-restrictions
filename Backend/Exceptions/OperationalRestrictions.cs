namespace Backend.Exceptions;

public class OperationalRestrictionNotFoundException : Exception
{
    public OperationalRestrictionNotFoundException(Guid id)
        : base($"Operational restriction was not found {id}")
    {
    }
}

public class OperationalRestrictionNotSignableException : Exception
{
    public OperationalRestrictionNotSignableException(Guid id, string status)
        : base($"Operational restriction {id} cannot be signed because it is {status}")
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

public class MissingRequiredRestrictionFieldException : Exception
{
    public MissingRequiredRestrictionFieldException(string fieldName)
        : base($"{fieldName} is required")
    {
    }
}

// Separation of duties: the user who created a restriction may hold a role
// that allows approving restrictions, but must not approve their own.
public class SelfApprovalNotAllowedException : Exception
{
    public SelfApprovalNotAllowedException(Guid restrictionId, Guid userId)
        : base($"User {userId} cannot approve operational restriction {restrictionId} because they created it")
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

public class NotControlRoomSupervisorException : Exception
{
    public NotControlRoomSupervisorException(Guid userId)
        : base($"User {userId} is not a control room supervisor and cannot sign for implementation")
    {
    }
}

public class NotDutyOperationsManagerException : Exception
{
    public NotDutyOperationsManagerException(Guid userId)
        : base($"User {userId} is not a duty operations manager and cannot approve restrictions")
    {
    }
}

public class NotOriginatorException : Exception
{
    public NotOriginatorException(Guid restrictionId, Guid userId)
        : base($"User {userId} is not the originator of operational restriction {restrictionId} and cannot start its cancellation")
    {
    }
}
