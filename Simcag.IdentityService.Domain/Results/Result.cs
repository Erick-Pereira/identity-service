namespace Simcag.IdentityService.Domain.Results;

/// <summary>
/// Result Pattern para tratamento estruturado de sucesso/erro.
/// </summary>
public abstract record Result
{
    public sealed record Success : Result;
    public sealed record Failure(string Error) : Result;

    public static Result Ok() => new Success();
    public static Result Fail(string error) => new Failure(error);

    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<string, TResult> onFailure) =>
        this switch
        {
            Success => onSuccess(),
            Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException()
        };

    public async Task<TResult> MatchAsync<TResult>(
        Func<Task<TResult>> onSuccess,
        Func<string, Task<TResult>> onFailure) =>
        this switch
        {
            Success => await onSuccess(),
            Failure f => await onFailure(f.Error),
            _ => throw new InvalidOperationException()
        };
}

/// <summary>
/// Result Pattern com valor de retorno.
/// </summary>
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string Error) : Result<T>;

    public static Result<T> Ok(T value) => new Success(value);
    public static Result<T> Fail(string error) => new Failure(error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure) =>
        this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => onFailure(f.Error),
            _ => throw new InvalidOperationException()
        };

    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<string, Task<TResult>> onFailure) =>
        this switch
        {
            Success s => await onSuccess(s.Value),
            Failure f => await onFailure(f.Error),
            _ => throw new InvalidOperationException()
        };

    public void Match(
        Action<T> onSuccess,
        Action<string> onFailure)
    {
        switch (this)
        {
            case Success s:
                onSuccess(s.Value);
                break;
            case Failure f:
                onFailure(f.Error);
                break;
        }
    }

    public async Task MatchAsync(
        Func<T, Task> onSuccess,
        Func<string, Task> onFailure)
    {
        switch (this)
        {
            case Success s:
                await onSuccess(s.Value);
                break;
            case Failure f:
                await onFailure(f.Error);
                break;
        }
    }
}
