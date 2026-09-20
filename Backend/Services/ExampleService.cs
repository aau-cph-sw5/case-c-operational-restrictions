namespace Backend.Services;

// SERVICE (MVCS):
// Encapsulates core business logic, domain rules, and operations
// Registered with Dependency Injection and injected into Controllers
public interface IExampleService
{
    void HandleExampleRequest();
}

public class ExampleService : IExampleService
{
    public bool HandleExampleRequest()
    {
        Console.WriteLine("ExampleService: received request from frontend");
        return true;
    }
}
