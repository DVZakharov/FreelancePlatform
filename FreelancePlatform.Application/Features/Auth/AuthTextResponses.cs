namespace FreelancePlatform.Application.Features.Auth;

public static class AuthTextResponses
{
    public const string InvalidCredentials = "Неверные учетные данные.";

    public const string LoginSuccessful = "Вход выполнен успешно.";
    public const string LogoutSuccessful = "Выход выполнен успешно.";
    public const string RefreshTokenSuccessful = "Токен обновлен успешно.";

    public const string EmailAlreadyExists = "Пользователь с таким адресом электронной почты уже зарегистрирован.";
    public const string RegistrationSuccessful = "Пользователь успешно зарегистрирован.";

    public const string RefreshTokenExpired = "Токен обновления истек.";
    public const string InvalidRefreshToken = "Неверный токен обновления.";


    public const string ValidationFailed = "Ошибка валидации данных.";
    public const string InvalidEmailEmpty = "Адрес электронной почты не может быть пустым.";
    public const string InvalidPasswordEmpty = "Пароль не может быть пустым.";
    public const string InvalidEmail = "Некорректный формат адреса электронной почты.";
    public const string InvalidPassword8 = "Пароль должен содержать не менее 8 символов.";
    public const string InvalidPassword100 = "Пароль должен содержать не более 100 символов.";
    public const string InvalidPasswordUppercase = "Пароль должен содержать хотя бы одну заглавную букву (A-Z).";
    public const string InvalidPasswordLowercase = "Пароль должен содержать хотя бы одну строчную букву (a-z).";
    public const string InvalidPasswordDigit = "Пароль должен содержать хотя бы одну цифру (0-9).";
    public const string InvalidPasswordSpecialChar = "Пароль должен содержать хотя бы один специальный символ (!@#$%^&* и т.п.).";
    public const string InvalidFullName = "Имя пользователя должно содержать от 2 до 200 символов.";
}