namespace FreelancePlatform.Application.Features.Messages;

public static class MessageTextResponses
{
    public const string ContractNotFound = "Контракт не найден";
    public const string NotParticipant = "Вы не являетесь участником этого контракта";
    public const string Sent = "Сообщение успешно отправлено";
    public const string GetSuccess = "Сообщения успешно получены";

    public const string InvalidContractId = "Неверный формат идентификатора контракта";
    
    public const string ValidationError = "Ошибка валидации";
    public const string MessageEmpty = "Сообщение не может быть пустым";
    public const string MessageMaximumSizeError = "Максимальный размер сообщения 2000 символов";
}