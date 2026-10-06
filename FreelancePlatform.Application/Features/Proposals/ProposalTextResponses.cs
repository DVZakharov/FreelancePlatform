namespace FreelancePlatform.Application.Features.Proposals;

public static class ProposalTextResponses
{
    public const string ProjectNotFound = "Проект не найден";
    public const string ProjectNotOpen = "Проект не принимает отклики";
    public const string NoFreelancerProfile = "Необходим профиль фрилансера для создания отклика";
    public const string CannotApplyToOwnProject = "Вы не можете подать заявку на участие в собственном проекте";
    public const string AlreadyApplied = "Вы уже отправили предложение по этому проекту";
    public const string NotFound = "Отклик не найден";
    public const string NotOwnerOfProject = "Вы можете управлять откликами только в своих проектах";
    public const string NotPending = "Это предложение уже обработано";
    public const string Submitted = "Предложение успешно отправлено";
    public const string Accepted = "Отклик принят, контракт создан";
    public const string Rejected = "Отклик отклонён";
    public const string GetSuccess = "Отклик получен успешно";


    public const string ValidationError = "Ошибка валидации отклика";

    public const string PriceGreaterZero = "Цена должна быть больше 0.";
    public const string EstimatedDaysGreaterZero = "Ожидаемых дней должно быть больше 0.";
    public const string CoverLetterNotEmpty = "Сопроводительное письмо не может быть пустым.";
    public const string CoverLetterMaxLenght = "Сопроводительное письмо не может быть более 3000 символов.";
}