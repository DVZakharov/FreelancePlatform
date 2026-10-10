using FluentValidation;

namespace FreelancePlatform.Tests.Helpers;

public static class ValidatorAssert
{
    public static void FailsOn<T>(this IValidator<T> validator, T model, string property)
    {
        var result = validator.Validate(model);
        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    public static void Passes<T>(this IValidator<T> validator, T model)
    {
        var result = validator.Validate(model);
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }
}