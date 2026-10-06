namespace FreelancePlatform.Application.Features.Payments;

public static class PaymentTextResponses
{
    public const string ValidationError = "Ошибка валидации";
    public const string TopUpSuccess = "Баланс успешно пополнен";
    public const string HistoryRetrieved = "История транзакций успешно получена";
    public const string BalanceRetrieved = "Баланс успешно получен";

    public const string AmountBelowZero = "Сумма должна быть больше 0";
    public const string AmountGreaterMillion = "Сумма должна быть меньше 1_000_000";
}