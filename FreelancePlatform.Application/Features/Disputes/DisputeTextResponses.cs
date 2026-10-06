namespace FreelancePlatform.Application.Features.Disputes;

public static class DisputeTextResponses
{
    public const string ContractNotFound = "Контракт не найден";
    public const string ContractNotActive = "Дискусию можно открыть только для активных контрактов";
    public const string NotParticipant = "Вы не являетесь участником этого контракта";
    public const string NotFound = "Дискусия не найдена";
    public const string NotAdmin = "Только администраторы могут разрешать дискусии";
    public const string NotOpen = "Эта дискусия уже разрешен";
    public const string InvalidDecision = "Решение должно быть либо «Клиент», либо «Фрилансер»";
    public const string Opened = "Дискусия успешно открыта";
    public const string Resolved = "Дискусия успешно разрешена";
    public const string GetSuccess = "Дискусия успешно получена";

    public const string InvalidContractId = "Неверный формат идентификатора контракта";
    public const string InvalidId = "Неверный формат идентифиактора дискусии";
    
    public const string ValidationError = "Ошибка валидации";
    public const string ReasonNotNull = "Требуется причина для открытия дискуссии";
    public const string ReasonMinimumSizeError = "Минимальный размер причины 20 символов";
    public const string ReasonMaximumSizeError = "Максимальный размер причины 2000 символов";
    public const string ResolutionEmpty = "Решение не может быть пустым";
    public const string ResolutionMaximumSizeError = "Максимальный размер решения 2000 символов";
}