namespace LegacyRenewalApp.Services;

public interface ISupportFeeCalculator
{
    decimal Calculate(string normalizedPlanCode);
}