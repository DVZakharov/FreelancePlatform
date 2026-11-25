namespace FreelancePlatform.Application.Features.Admin;

public static class AdminTextResponses
{
    public const string InvalidSecret = "Неверный секрет настройки";
    public const string UserNotFound = "Пользователь не найден";
    public const string AlreadyAdmin = "Пользователь уже администратор";
    public const string NotAnAdmin = "Пользователь не администратор";
    public const string CannotDemoteLastAdmin = "Невозможно понизить в должности последнего оставшегося администратора";
    public const string Promoted = "Пользователь успешно повышен до администратора";
    public const string Demoted = "Пользователь успешно понижен в правах с администратора";
    public const string UsersRetrieved = "Пользователи успешно получены";
    public const string UserRetrieved = "Пользователь успешно получен";
    public const string DisputesRetrieved = "Споры успешно получены";
    public const string StatsRetrieved = "Статистика платформы успешно получена";

    public const string ValidationError = "Ошибка валидации запроса";
    public const string EmailNotEmpty = "Указание почты обязательно";
    public const string EmailNotValid = "Не верный формат почты";
    public const string InvalidUserId = "Неверный формат идентификатор пользователя";
    public const string RoleSelectError = "Роль может быть только из списка: Client, Freelancer, Admin";
    public const string StatusSelectError = "Статус может быть только из списка: Open, ResolvedForClient, ResolvedForFreelancer, Closed";
}