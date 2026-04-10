using LegacyRenewalApp.Models;
using LegacyRenewalApp.Repositories;
using System;

namespace LegacyRenewalApp.Services
{
    public class SubscriptionRenewalService(
        CustomerRepository customerRepository, 
        SubscriptionPlanRepository planRepository,
        IDiscountCalculator discountCalculator,
        ITaxCalculator taxCalculator,
        ISupportFeeCalculator supportFeeCalculator,
        IPaymentFeeCalculator paymentFeeCalculator,
        IBillingGateway billingGateway)
    {
        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            //  Validation
            if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
            if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code is required");
            if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required");

            //  Input normalization
            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            //  Retrieving data from repository
            var customer = customerRepository.GetById(customerId);
            var plan = planRepository.GetByCode(normalizedPlanCode);

            //  Validating retrieved data with context of current operation
            if (!customer.IsActive) throw new InvalidOperationException("Inactive customers cannot renew subscriptions");

            // Base price + discounts
            var baseAmount = plan.MonthlyPricePerSeat * seatCount * 12m + plan.SetupFee;
            var (discountAmount, notes) =
                discountCalculator.Calculate(customer, plan, seatCount, baseAmount, useLoyaltyPoints);

            var subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }

            //  Support fee
            var supportFee = 0m;
            if (includePremiumSupport)
            {
                supportFee = supportFeeCalculator.Calculate(normalizedPlanCode);
                notes += "premium support included; ";
            }

            //  Payment method fee
            var (paymentFee, paymentNote) = paymentFeeCalculator.Calculate(paymentMethod.ToUpperInvariant(),
                subtotalAfterDiscount + supportFee);
            notes += paymentNote;
            
            //  Tax calc
            var taxRate = taxCalculator.GetRate(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            //  Making sure that we will get enough money for our boss so that he can have fun
            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            //  Create and store - we are very cl,ose to the end
            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };
            billingGateway.SaveInvoice(invoice);

            //  Send invoice
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body =
                    $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                    $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

                billingGateway.SendEmail(customer.Email, subject, body);
            }

            //  Done
            return invoice;
        }
    }
}
