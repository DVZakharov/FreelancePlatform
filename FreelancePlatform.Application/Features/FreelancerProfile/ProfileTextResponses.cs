namespace FreelancePlatform.Application.Features.FreelancerProfile;

public static class ProfileTextResponses
{
    public const string Created = "Профиль фрилансера успешно создан.";
    public const string Updated = "Профиль фрилансера успешно обновлен.";

    public const string SearchSuccess = "Пооис выполнен успешно";

    public const string Deleted = "Профиль фрилансера успешно удален.";
    public const string NotFound = "Профиль фрилансера не найден.";
    public const string GetSuccess = "Профиль фрилансера успешно получен.";
    public const string AlreadyExists = "Профиль фрилансера уже существует.";
    public const string NotOwner = "Вы не владеелец профиля";

    public const string SkillsNotFound = "Один или несколько навыков не найдены.";

    public const string ValidationError = "Ошибка валидации профиля фрилансера.";
    public const string BioEmpty = "Биография не может быть пустой.";
    public const string BioTooShort = "Биография должна содержать не менее 50 символов.";
    public const string BioTooLong = "Биография не должна превышать 2000 символов.";
    public const string HourlyRateInvalid = "Почасовая ставка должна быть больше 0.";
    public const string SkillIdsEmpty = "Выберите хотя бы один навык.";
}