namespace FreelancePlatform.Application.Features.Reviews;

public static class ReviewTextResponses
{
    public const string ValidationError = "Ошибка валидации";
    public const string ContractNotFound = "Контракт не найден";
    public const string ContractNotCompleted = "Отзывы можно оставлять только на завершённые контракты";
    public const string NotParticipant = "Вы не являетесь участником этого контракта";
    public const string AlreadyReviewed = "Вы уже оставили отзыв об этом договоре";
    public const string GetSuccess = "Отзывы успешно получены";
    public const string Created = "Отзыв успешно отправлен";
    public const string ProfileNotFound = "Профиль фрилансера не найден";

    public const string InvalidContractId = "Неверный формат идентификатора контракта";
    public const string InvalidProfileId = "Неверный формат идентификатора профиля фрилансера";
}