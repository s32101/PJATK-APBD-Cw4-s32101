namespace LegacyRenewalApp.Services;

public class SupportFeeCalculator : ISupportFeeCalculator
{
    public decimal Calculate(string normalizedPlanCode)
    {
        return normalizedPlanCode switch
        {
            "START" => 250m,
            "PRO" => 400m,
            "ENTERPRISE" => 700m,
            _ => 0
        };
    }
}