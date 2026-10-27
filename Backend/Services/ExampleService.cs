namespace Backend.Services;

public class ExampleService : IExampleService
{
    public bool HandleExampleRequest()
    {
        Console.WriteLine("ExampleService: received request from frontend");
        return true;
    }
}
