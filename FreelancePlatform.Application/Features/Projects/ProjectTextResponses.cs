namespace FreelancePlatform.Application.Features.Projects;

public static class ProjectTextResponses
{
    public const string CategoryNotFound = "Категория не найдена";
    public const string SkillsNotFound = "Один или более умений не найдено";
    public const string NotFound = "Проект не найден";
    public const string NotOwner = "Вы можете изменять только свой проект";
    public const string NotDraft = "Проект можно опубликовать только из состояния черновика";
    public const string CannotEdit = "Проект можно редактировать только в статусе черновика или открытого проекта.";
    public const string CannotCancel = "Проект можно отменить только в статусе черновика или открытого проекта.";
    public const string Created = "Проек создан успешно";
    public const string Published = "Проект опубликован успешно";
    public const string Updated = "Проект обновлен успешно";
    public const string Cancelled = "Проект успешно отменён";
    public const string GetSuccess = "Проект получен успешно";
    public const string SearchSuccess = "Список проектов получен успешно";

    public const string InvalidId = "Невалидный идентификатор проекта";


    public const string ValidationError = "Ошибка валидации проекта";

    public const string InvalidTitleMaxLenght = "Титульник проекта должен содержать до 200 символов.";
    public const string TitleRequired = "Титульник проекта должен быть не пустым";
    public const string InvalidTitleMinLenght = "Титульник проекта должен содержать от 1 символа.";

    public const string InvalidDescriptionMaxLenght = "Описание проекта должен содержать до 5000 символов.";
    public const string InvalidDescriptionMinLenght = "Описание проекта должно содержать не менее 50 символов.";

    public const string InvalidBudgetMin = "Минимальный размер бюджета должен быть не менее 0.";
    public const string InvalidBudgetMax = "Максимальный размер бюджета должен быть не менее минимального.";
    public const string InvalidBudget = "Бюджет должен быть не менее нуля.";

    public const string InvalidDeadline = "Крайний срок должен быть в будущем";

    public const string OneOrMoreRequiredSkills = "Должен быть указан хотя бы один обязательный навык.";
}