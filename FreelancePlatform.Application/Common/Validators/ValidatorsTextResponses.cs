namespace FreelancePlatform.Application.Common.Validators;

public static class ValidatorsTextResponses
{
    public const string PageNumberInvalid = "Номер страницы должен быть больше 0.";
    public const string PageSizeInvalid = "Размер страницы должен быть больше 0.";

    public const string InvalidNameEmpty = "Название не может быть пустым.";
    public const string InvalidNameLength = "Название должно содержать от 2 до 100 символов.";
    public const string InvalidNameFormat = "Название может содержать только буквы, цифры и пробелы.";

    public const string InvalidId = "Некорректный идентификатор.";
    public const string OneOrMoreInvalidId = "Один или более идентификаторов некорректны.";
    public const string InvalidIdEmpty = "Идентификатор не может быть пустым.";

    public const string InvalidRating = "Оценка должна быть в диапазоне от 0 да 5";
}