namespace Backend.Services.Email;

public interface IEmailService
{
    Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default);
}
