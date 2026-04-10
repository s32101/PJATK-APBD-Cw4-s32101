using LegacyRenewalApp.Models;

namespace LegacyRenewalApp.Services;

public interface IDiscountCalculator
{
    (decimal discount, string notes) Calculate(Customer customer, SubscriptionPlan plan, int seatCount,
        decimal baseAmount, bool usePoints);
}