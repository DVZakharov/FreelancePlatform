namespace FreelancePlatform.Application.Features.Categories;

public static class CategoriesTextResponses
{
    public const string Updated = "Категория успешно обновлена.";
    public const string Deleted = "Категория успешно удалена.";
    public const string NotFound = "Категория не найдена.";
    public const string AlreadyExists = "Категория с таким названием уже существует.";
    public const string GetSuccess = "Категория успешно получена.";
    public const string GetsSuccess = "Список категорий успешно получен.";

    public static string CreatedWithName(string name) => $"Категория \"{name}\" успешно создана.";
    public static string DeletedWithName(string name) => $"Категория \"{name}\" успешно удалена.";

    /// <summary>
    /// Сообщение об ошибке валидации данных категории.
    /// </summary>
    public const string ValidationFailed = "Ошибка валидации данных категории.";
    public const string InvalidId = "Неверный формат идентифиактора категории";
}