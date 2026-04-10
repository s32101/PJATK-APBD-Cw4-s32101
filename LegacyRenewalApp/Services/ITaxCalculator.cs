namespace LegacyRenewalApp.Services;

public interface ITaxCalculator
{
    decimal GetRate(string country);
}