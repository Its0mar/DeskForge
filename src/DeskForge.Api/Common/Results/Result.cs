using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DeskForge.Api.Common.Results;

public static class Result
{
    public static Result<Success> Success => new Success();
    public static Result<Created> Created => new Created();
    public static Result<Deleted> Deleted => new Deleted();
    public static Result<Updated> Updated => new Updated();
}

public sealed class Result<TValue> : IResult<TValue>
{
    private readonly TValue? _value = default;
    private readonly List<Error>? _errors = null;
    
    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public List<Error>? Errors  => IsError ? _errors : [];
    public TValue? Value => IsSuccess ? _value : default;
    public Error TopError => (_errors?.Count > 0) ? _errors[0] : default;


    private Result(Error error)
    {
        _errors = [error];
        IsSuccess = false;
    }

    [JsonConstructor]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("For serializer only. Do not use in application code.")]
    public Result(TValue? value, List<Error>? errors, bool isSuccess)
    {
        if (isSuccess)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _errors = [];
            IsSuccess = true;
        }
        else
        {
            if (errors == null || errors.Count == 0)
            {
                throw new ArgumentException("Provide at least one error.", nameof(errors));
            }

            _errors = errors;
            _value = default!;
            IsSuccess = false;
        }
    }
    
    private Result(List<Error> errors)
    {
        if (errors is null || errors.Count == 0)
        {
            throw new ArgumentException("At least one error must be specified.", nameof(errors));
        }
        
        _errors = errors;
        IsSuccess = false;
    }
    
    private Result(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        _value = value;
        IsSuccess = true;
    }
    
    public TNextValue Match<TNextValue>(Func<TValue, TNextValue> onValue, Func<List<Error>, TNextValue> onError) =>
        IsSuccess ? onValue(_value!) : onError(_errors!);

    public static implicit operator Result<TValue>(TValue value)
        => new(value);
    
    public static implicit operator Result<TValue>(Error error)
        => new(error);
    
    public static implicit operator Result<TValue>(List<Error> errors)
        => new(errors);
}

public readonly record struct Success;
public readonly record struct Created;
public readonly record struct Deleted;
public readonly record struct Updated;

