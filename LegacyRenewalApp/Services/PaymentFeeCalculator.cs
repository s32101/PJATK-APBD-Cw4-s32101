using System;

namespace LegacyRenewalApp.Services;

public class PaymentFeeCalculator : IPaymentFeeCalculator
{
    public (decimal, string) Calculate(string method, decimal amount)
    {
        return method switch
        {
            "CARD" => (amount * 0.02m, "card payment fee; "),
            "BANK_TRANSFER" => (amount * 0.01m, "bank transfer fee; "),
            "PAYPAL" => (amount * 0.035m, "paypal fee; "),
            "INVOICE" => (0m, "invoice payment; "),
            _ => throw new ArgumentException("Unsupported payment method")
        };
    }
}