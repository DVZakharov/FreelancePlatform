using FluentValidation;

namespace FreelancePlatform.Application.Common.Validators;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidEnum<T, TEnum>(this IRuleBuilder<T, string> ruleBuilder)
        where TEnum : struct, Enum
    {
        return ruleBuilder
            .Must(value => Enum.TryParse<TEnum>(value, true, out _))
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", Enum.GetNames(typeof(TEnum)))}");
    }

    public static IRuleBuilderOptions<T, IEnumerable<string>?> MustAllBeValidGuids<T>(
        this IRuleBuilder<T, IEnumerable<string>?> ruleBuilder)
    {
        return ruleBuilder
            .Must(values => values is null || values.All(v => Guid.TryParse(v, out _)))
            .WithMessage(ValidatorsTextResponses.OneOrMoreInvalidId);
    }

    public static IRuleBuilderOptions<T, string?> MustBeValidGuid<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotNull()
            .NotEmpty().WithMessage(ValidatorsTextResponses.InvalidIdEmpty)
            .Must(id => Guid.TryParse(id, out _)).WithMessage(ValidatorsTextResponses.InvalidId);
    }

    public static IRuleBuilderOptions<T, string> MustBeValidName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage(ValidatorsTextResponses.InvalidNameEmpty)
            .MinimumLength(2).WithMessage(ValidatorsTextResponses.InvalidNameLength)
            .MaximumLength(100).WithMessage(ValidatorsTextResponses.InvalidNameLength)
            .Must(v => v.All(c => !char.IsControl(c))).WithMessage(ValidatorsTextResponses.InvalidNameFormat);
    }

    public static IRuleBuilderOptions<T, int> MustBeValidPageNumber<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0).WithMessage(ValidatorsTextResponses.PageNumberInvalid);
    }

    public static IRuleBuilderOptions<T, int> MustBeValidPageSize<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0).WithMessage(ValidatorsTextResponses.PageSizeInvalid);
    }
}