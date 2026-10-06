namespace Backend.Exceptions;

public class OperationalRestrictionNotFoundException : Exception
{
    public OperationalRestrictionNotFoundException(Guid id) 
        : base($"Operational restriction was not found {id}") 
    {
    }
}