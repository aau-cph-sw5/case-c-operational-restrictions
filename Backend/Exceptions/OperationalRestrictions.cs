namespace Backend.Exceptions;

public class OperationalRestrictionNotFoundException : Exception
{
    public OperationalRestrictionNotFoundException(Guid id) 
        : base($"Operational restriction was not found {id}")
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