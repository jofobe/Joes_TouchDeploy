namespace JoesTouchDeploy.Core.Models;

public class OperationResult<T>
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public T? Data { get; init; }

    public Exception? Exception { get; init; }
}
