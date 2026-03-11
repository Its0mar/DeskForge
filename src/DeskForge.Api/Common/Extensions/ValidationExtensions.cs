using DeskForge.Api.Common.Results;
using FluentValidation.Results;

namespace DeskForge.Api.Common.Extensions;

public static class ValidationExtensions
{
    public static List<Error> ToErrors(this ValidationResult result)
        => result.Errors
            .Select(e => Error.Validation(e.PropertyName, e.ErrorMessage))
            .ToList();
}