namespace DeskForge.Api.Common.Results;

public interface IResult
{
    bool IsSuccess { get; }
    List<Error>? Errors { get; }
}

public interface IResult<out TValue> : IResult
{
    TValue? Value { get; }
}