namespace FreelancePlatform.Application.Features.Milestones;

public static class MilestoneTextResponses
{
    public const string ContractNotFound = "Контракт не найден";
    public const string ContractNotActive = "Контракт неактивен";
    public const string NotClient = "Только клиент может выполнить это действие";
    public const string NotFreelancer = "Только фрилансер может выполнить это действие";
    public const string NotParticipant = "Вы не являетесь участником этого договора";
    public const string NotFound = "Этап не найден";
    public const string AlreadyFunded = "Этап уже профинансирован";
    public const string NotFunded = "Этап должен быть профинансирована до этого действия";
    public const string NotSubmitted = "Этап должен быть отправлен до этого действия";
    public const string InsufficientBalance = "Недостаточно средств";
    public const string Created = "Этап успешно создан";
    public const string Funded = "Этап успешно профинансирован";
    public const string Submitted = "Этап успешно отправлен";
    public const string Approved = "Этап пройден, средства переведены фрилансеру";
    public const string Rejected = "Этап отклонен, отправлен на доработку";
    public const string GetSuccess = "Этапы успешно получены";

    public const string InvalidContractId = "Неверный формат идентификатора контракта";
    public const string InvalidId = "Неверный формат идентификатора этапа";
    
    public const string ValidationError = "Ошибка валидации";
    public const string TitleEmpty = "Заголовок этапа не может быть пустым";
    public const string TitleMaxSizeError = "Заголовок этапа не может превышать 2000 символов";
    public const string PriceBelowZeroError = "Стоимость этапа не может быть меньше 0";
    public const string ReasonEmpty = "Требуется причина для отклонения этапа";
    public const string ReasonMaximumSizeError = "Максимальный размер причины 1000 символов";
}