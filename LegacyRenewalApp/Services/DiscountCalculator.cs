using System;
using LegacyRenewalApp.Models;

namespace LegacyRenewalApp.Services;

public class DiscountCalculator : IDiscountCalculator
{
    public (decimal, string) Calculate(Customer customer, SubscriptionPlan plan, int seatCount, decimal baseAmount, bool usePoints)
    {
        var discount = 0m;
        var notes = "";

        // segment
        discount += CalculateSegmentDiscount(customer, plan, baseAmount, ref notes);

        // loyalty
        discount += CalculateLoyaltyDiscount(customer, baseAmount, ref notes);

        // seats
        discount += CalculateSeatDiscount(seatCount, baseAmount, ref notes);

        // points
        if (usePoints && customer.LoyaltyPoints > 0)
        {
            var points = Math.Min(customer.LoyaltyPoints, 200);
            discount += points;
            notes += $"loyalty points used: {points}; ";
        }

        return (discount, notes);
    }

    private static decimal CalculateSegmentDiscount(Customer c, SubscriptionPlan plan, decimal baseAmount, ref string notes)
    {
        return c.Segment switch
        {
            "Silver" => Add(baseAmount * 0.05m, "silver discount; ", ref notes),
            "Gold" => Add(baseAmount * 0.10m, "gold discount; ", ref notes),
            "Platinum" => Add(baseAmount * 0.15m, "platinum discount; ", ref notes),
            "Education" when plan.IsEducationEligible => Add(baseAmount * 0.20m, "education discount; ", ref notes),
            _ => 0
        };
    }

    private static decimal CalculateLoyaltyDiscount(Customer c, decimal baseAmount, ref string notes)
    {
        return c.YearsWithCompany switch
        {
            >= 5 => Add(baseAmount * 0.07m, "long-term loyalty discount; ", ref notes),
            >= 2 => Add(baseAmount * 0.03m, "basic loyalty discount; ", ref notes),
            _ => 0
        };
    }

    private static decimal CalculateSeatDiscount(int seats, decimal baseAmount, ref string notes)
    {
        return seats switch
        {
            >= 50 => Add(baseAmount * 0.12m, "large team discount; ", ref notes),
            >= 20 => Add(baseAmount * 0.08m, "medium team discount; ", ref notes),
            >= 10 => Add(baseAmount * 0.04m, "small team discount; ", ref notes),
            _ => 0
        };
    }

    private static decimal Add(decimal value, string note, ref string notes)
    {
        notes += note;
        return value;
    }
}