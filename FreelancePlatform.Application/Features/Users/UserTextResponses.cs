namespace FreelancePlatform.Application.Features.Users;

public class UserTextResponses
{
    public const string NotFound = "Пользователь не найден";
    public const string ValidationError = "Ошибка валидации";
    public const string GetActiveSessionsSuccess = "Активные сессии успешно получены.";

    public const string GetSuccess = "Пользователь успешно извлечён";
    public const string Updated = "Пользователь успешно обновлен";
    public const string NotOwnSession = "Вы можете отзывать только свои собственные сеансы";
    
    public const string AllSessionsRevoked = "Все сеансы успешно отменены";
    
    public const string ForbiddenAccess = "Недостаточно прав для выполнения этого действия.";
    public const string UnauthorizedAccess = "Доступ запрещен. Требуется аутентификация.";
    
    
    public const string SessionNotFound = "Сессия не найдена.";
    public const string SessionRevoked = "Сессия успешно отозвана.";
}
