namespace Backend.Exceptions;

public class InvalidNotificationException : Exception
{
    public InvalidNotificationException(string message)
        : base(message)
    {
    }
}
