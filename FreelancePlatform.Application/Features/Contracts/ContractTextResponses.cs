namespace FreelancePlatform.Application.Features.Contracts;

public static class ContractTextResponses
{
    public const string ValidationError = "Ошибка валидации";
    public const string NotFound = "Контракт не найден";
    public const string NotClient = "Только клиент может выполнить это действие";
    public const string NotActive = "Контракт неактивен";
    public const string MilestonesNotCompleted = "Все этапы должны быть завершены до окончания срока действия контракта";
    public const string Completed = "Контракт успешно выполнен";
    public const string NotParticipant = "Вы не являетесь участником этого контракта";
    public const string GetSuccess = "Контракт успешно получен";

    public const string InvalidId = "Неверный формат идентификатора контракта";
}