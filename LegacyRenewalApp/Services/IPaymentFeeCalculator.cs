namespace LegacyRenewalApp.Services;

public interface IPaymentFeeCalculator
{
    (decimal fee, string note) Calculate(string method, decimal amount);
}