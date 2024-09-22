namespace RazorStatic.ConsoleApp.Services;

public interface ITestService
{
    string Message { get; }
}

internal sealed class TestService
    : ITestService
{
    public string Message => $"Hello from {nameof(TestService)}!!!!";
}